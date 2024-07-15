using AngleSharp;
using cardscore_api.Models;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium;
using System.Globalization;
using OpenQA.Selenium.Support.UI;
using System.Collections.ObjectModel;
using AngleSharp.Dom;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Chrome;
using System.Text.Json;
using AngleSharp.Text;
namespace cardscore_api.Services.ParserServices
{
    public class SoccerwayParserService
    {
        private readonly string _domain = "https://ru.soccerway.com";
        private readonly int _timeFix = -2;
        private readonly FormatService _formatService;
        private readonly DateTime _startDate = DateTime.UtcNow.AddDays(-4);
        private readonly AngleSharp.IConfiguration _configuration;
        private readonly IBrowsingContext _parserContext;
        private readonly FetchService _fetchService;
        private readonly FirefoxOptions _driverOptions;
        private readonly ErrorsService _errorsService;
        private readonly RedisService _redisService;

        public SoccerwayParserService(FormatService formatService, FetchService fetchService, ErrorsService errorsService, RedisService redisService)
        {
            _fetchService = fetchService;
            _formatService = formatService;

            FirefoxProfile profile = new FirefoxProfile();
            profile.SetPreference("browser.cache.disk.enable", false);
            profile.SetPreference("browser.cache.memory.enable", false);
            profile.SetPreference("browser.cache.offline.enable", false);
            profile.SetPreference("network.http.use-cache", false);
            profile.SetPreference("extensions.enabled", false);
            profile.SetPreference("browser.privatebrowsing.autostart", true);
            profile.SetPreference("permissions.default.stylesheet", 2);
            profile.SetPreference("permissions.default.image", 2);

            _driverOptions = new FirefoxOptions();
            _driverOptions.Profile = profile;
            _driverOptions.PageLoadStrategy = PageLoadStrategy.Eager;
            _driverOptions.AddArgument("--headless");

            _driverOptions.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:127.0) Gecko/20100101 Firefox/127.0");
            _errorsService = errorsService;
            _redisService = redisService;
        }

        public async Task<League> GetLeagueDataByUrl(string url)
        {
            var driver = new FirefoxDriver(_driverOptions);

            League league = new();

            try
            {
                driver.Navigate().GoToUrl(url.Contains(_domain) ? url : _domain + url);

                league = GetBaseLeagueInfo(driver);

                league.Url = url;

                return league;
            }
            catch (Exception ex)
            {
                _errorsService.CreateErrorFile(ex);

            }
            finally
            {
                driver.Close();
                driver.Quit();
            }

            return league;
        }

        public League GetBaseLeagueInfo(WebDriver document)
        {
            var titleElem = document.FindElement(By.CssSelector("h1"));

            var title = _formatService.ClearString(titleElem.Text);

            var countryElem = document.FindElement(By.CssSelector("h2"));

            var country = _formatService.ClearString(countryElem.Text);

            League league = new()
            {
                Title = title + " " + country,
                Country = country,
            };

            return league;
        }

        public async Task<List<Game>> GetGames(WebDriver driver, string leagueName, string leagueUrl, bool parseActions = true, bool onlyActive = false, DateTime? startDateFilter = null, DateTime? endDateFilter = null)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

            var isPaged = driver.FindElements(By.CssSelector("select.page-dropdown")).Count > 0;

            var pagesElem = isPaged ? driver.FindElements(By.CssSelector("select.page-dropdown option")) : null;

            var pagesCount = pagesElem != null ? pagesElem.Count : 1;

            var isPaginated = driver.FindElements(By.CssSelector(".block_competition_matches_summary .next")).Count > 0 && driver.FindElements(By.CssSelector(".block_competition_matches_summary .previous")).Count > 0;

            var count = isPaged ? pagesCount : isPaginated ? Int32.MaxValue : 1;

            List <Game> games = new();

            for (int i = 0; i < count; i++)
            {

                if (isPaged)
                {
                    driver.ExecuteScript($"document.querySelector('select.page-dropdown').value = '{i}'; document.querySelector('select.page-dropdown').dispatchEvent(new CustomEvent('change'))");
                    wait.Until(d => d.FindElements(By.CssSelector(".content .overlay")).Count == 0);
                }

                var gameElems = driver.FindElements(By.CssSelector("tr.match"));

                if (gameElems == null)
                {
                    continue;
                }

                for (var j = 0; j < gameElems.Count; j++)
                {
                    var gameElemsList = driver.FindElements(By.CssSelector("tr.match"));
                    var gameElem = gameElemsList[j];

                    var activeGameElem = gameElem.GetAttribute("data-status");

                    string timeElement = gameElem.GetAttribute("data-timestamp");


                    string id = gameElem.GetAttribute("data-event-id");

                    DateTime dateTime = DateTime.UnixEpoch.AddSeconds(_formatService.ToInt(timeElement));

                    var isPlayedTwoHourAgo = dateTime >= DateTime.UtcNow.AddHours(-4) && dateTime <= DateTime.UtcNow;

                    var activeGame = activeGameElem == "Playing" || isPlayedTwoHourAgo;

                    if (onlyActive && !activeGame)
                    {
                        continue;
                    }

                    if (dateTime < startDateFilter || dateTime > endDateFilter)
                    {
                        continue;
                    }

                    var team1Elem = gameElem.FindElement(By.CssSelector(".team-a a"));
                    var team2Elem = gameElem.FindElement(By.CssSelector(".team-b a"));
                    var scoreElem = gameElem.FindElements(By.CssSelector(".extra_time_score"));

                    var team1Id = team1Elem.GetAttribute("href").Split('/')[^2];
                    var team2Id = team2Elem.GetAttribute("href").Split('/')[^2];

                    string[] scores = ["0", "0"];

                    if (scoreElem.Count > 0)
                    {
                        scores = scoreElem[0].Text.Split(" - ");
                    }

                    Team team1 = new()
                    {
                        Name = team1Elem.Text,
                        IconUrl = $"https://secure.cache.images.core.optasports.com/soccer/teams/150x150/{team1Id}.png",
                        Count = _formatService.ToInt(scores[0])
                    };

                    Team team2 = new()
                    {
                        Name = team2Elem.Text,
                        IconUrl = $"https://secure.cache.images.core.optasports.com/soccer/teams/150x150/{team2Id}.png",
                        Count = _formatService.ToInt(scores[1])
                    };

                    Team[] teams = [team1, team2];

                    var url = gameElem.FindElement(By.CssSelector(".score-time a")).GetAttribute("href");

                    string gameTime = null!;
                    List<GameAction> actions = new();

                    var pstpCheck = gameElem.GetAttribute("innerHTML").Contains("PSTP");

                    if ((gameTime == "" || gameTime == null) && pstpCheck)
                    {
                        gameTime = "Перенесен";

                    }
                    if (parseActions)
                    {
                        driver.Navigate().GoToUrl(url.Contains(_domain) ? url : _domain + url);

                        var countsNested = await ParseCounts(driver);

                        teams[0].Count = countsNested[0];
                        teams[1].Count = countsNested[1];

                        gameTime = await ParseTime(driver);

                        actions = await ParseActions(driver, leagueName, id, leagueUrl);

                        driver.Navigate().Back();
                    }

                    var game = new Game()
                    {
                        Id = id,
                        GameTime = gameTime,
                        DateTime = dateTime,
                        Teams = teams,
                        Url = url,
                        ActiveGame = activeGame,
                        Actions = actions
                    };

                    if (!games.Contains(game))
                    {
                        games.Add(game);
                    }
                }

                
                if (!isPaged && isPaginated)
                {
                    if (onlyActive)
                    {
                        if(i == 0)
                        {
                            var prevBtn = driver.FindElements(By.CssSelector(".block_competition_matches_summary .previous.disabled"));

                            if (prevBtn.Count > 0)
                            {
                                i++;
                                var nextBtn = driver.FindElements(By.CssSelector(".block_competition_matches_summary .next.disabled"));

                                if (nextBtn.Count > 0)
                                {
                                    break;
                                }

                                driver.ExecuteScript("document.querySelector('.block_competition_matches_summary .next')?.click()");
                                wait.Until(d => d.FindElements(By.CssSelector(".content .overlay")).Count == 0);
                            }
                            else
                            {
                                driver.ExecuteScript("document.querySelector('.block_competition_matches_summary .previous')?.click()");
                                wait.Until(d => d.FindElements(By.CssSelector(".content .overlay")).Count == 0);
                            }
                        }
                        else if (i == 1)
                        {
                            driver.ExecuteScript("document.querySelector('.block_competition_matches_summary .next')?.click()");
                            wait.Until(d => d.FindElements(By.CssSelector(".content .overlay")).Count == 0);

                            var nextBtn = driver.FindElements(By.CssSelector(".block_competition_matches_summary .next.disabled"));

                            if (nextBtn.Count > 0)
                            {
                                break;
                            }

                            driver.ExecuteScript("document.querySelector('.block_competition_matches_summary .next')?.click()");
                            wait.Until(d => d.FindElements(By.CssSelector(".content .overlay")).Count == 0);
                        }
                        else
                        {
                            break;
                        }
                        
                    }
                    else
                    {
                        if (i < 10000)
                        {
                            var prevBtn = driver.FindElements(By.CssSelector(".block_competition_matches_summary .previous.disabled"));

                            if (prevBtn.Count > 0)
                            {
                                i+= 10000;
                                driver.Navigate().Refresh();

                                var nextBtn = driver.FindElements(By.CssSelector(".block_competition_matches_summary .next.disabled"));

                                if (nextBtn.Count > 0)
                                {
                                    break;
                                }

                                driver.ExecuteScript("document.querySelector('.block_competition_matches_summary .next')?.click()");
                                wait.Until(d => d.FindElements(By.CssSelector(".content .overlay")).Count == 0);
                            }
                            else
                            {
                                driver.ExecuteScript("document.querySelector('.block_competition_matches_summary .previous')?.click()");
                                wait.Until(d => d.FindElements(By.CssSelector(".content .overlay")).Count == 0);
                            }

                        }
                        else
                        {
                            var nextBtn = driver.FindElements(By.CssSelector(".block_competition_matches_summary .next.disabled"));

                            if (nextBtn.Count > 0)
                            {
                                break;
                            }

                            driver.ExecuteScript("document.querySelector('.block_competition_matches_summary .next')?.click()");
                            wait.Until(d => d.FindElements(By.CssSelector(".content .overlay")).Count == 0);
                        }
                    }
                }

            }

            games.Sort((x, y) => x.DateTime.CompareTo(y.DateTime));

            return games;
        }

        public async Task<string> ParseTime(WebDriver driver)
        {
            var gametimeElem = driver.FindElements(By.CssSelector(".game-minute"));

            if (gametimeElem.Count > 0)
            {
                var gameTime = gametimeElem[0].Text;

                return gameTime;
            }

            return "";
        }

        public async Task<int[]> ParseCounts(WebDriver driver)
        {
            try
            {
                var countsElem = driver.FindElement(By.CssSelector(".match-info .container.middle")).Text;

                string[] lines = countsElem.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                string scoreLine = lines.FirstOrDefault(line => line.Contains("-"));

                if (scoreLine != null)
                {
                    string[] scores = scoreLine.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                    return new int[] { int.Parse(scores[0]), int.Parse(scores[1]) };
                }
                return [0, 0];
            }
            catch
            {
                return [0, 0];
            }
        }

        public async Task<List<GameAction>> ParseActionsByUrl(string url, string leagueName, string thisGameId, string leagueUrl)
        {
            var driver = new FirefoxDriver(_driverOptions);

            var actions = new List<GameAction>();

            try
            {
                driver.Navigate().GoToUrl(url.Contains(_domain) ? url : _domain + url);

                actions = await ParseActions(driver, leagueName, thisGameId, leagueUrl);
            }
            catch (Exception ex)
            {
                _errorsService.CreateErrorFile(ex);
            }
            finally
            {
                driver.Close();
                driver.Quit();
            }

            return actions;
        }

        public async Task<List<GameAction>> ParseActions(WebDriver driver, string leagueName, string thisGameId, string leagueUrl)
        {

            List<GameAction> actions = new();


            var leftTeamActions = await ParseActionsByContainer(leagueName, true, driver, thisGameId, leagueUrl);
            var rightTeamActions = await ParseActionsByContainer(leagueName, false, driver, thisGameId, leagueUrl);

            actions.AddRange(leftTeamActions);
            actions.AddRange(rightTeamActions);

          return actions;
        }

        public async Task<List<GameAction>> ParseActionsByContainer(string leagueName, bool leftTeam, WebDriver driver, string thisGameId, string leagueUrl)
        {
            List<GameAction> actions = new();

            var selector = leftTeam ? "left" : "right";

            var playerElemsCount = driver.FindElements(By.CssSelector($".combined-lineups-container .container.{selector} tr")).Count;

            for (var i = 0; i < playerElemsCount; i++)
            {
                var playerElems = driver.FindElements(By.CssSelector($".combined-lineups-container .container.{selector} tr"));

                var playerElem = playerElems[i];

                var bookings = playerElem.FindElements(By.CssSelector(".bookings span"));

                if (bookings.Count < 1)
                {
                    continue;
                }

                var gameAction = new GameAction();

                bookings.Reverse();

                var thisBooking = 0;

                for (var j = 0; j < bookings.Count; j++)
                {
                    var booking = bookings[j];
                    var src = booking.FindElement(By.CssSelector("img")).GetAttribute("src");

                    if (src.Contains("RC"))
                    {
                        gameAction.ActionType = GameActionType.RedCard;
                        thisBooking = j;
                    }
                    else if (src.Contains("Y2C"))
                    {
                        gameAction.ActionType = GameActionType.YellowRedCard;
                        thisBooking = j;
                    }
                    else if (src.Contains("YC"))
                    {
                        gameAction.ActionType = GameActionType.YellowCard;
                        thisBooking = j;
                    }
                }

                if (gameAction.ActionType == null)
                {
                    continue;
                }

                gameAction.Time = _formatService.ClearString(bookings[thisBooking].Text);

                Player player = new();

                var linkElem = playerElem.FindElement(By.CssSelector("a"));

                var name = String.Copy(_formatService.ClearString(linkElem.Text));

                player.Name = name;

                player.Url = String.Copy(linkElem.GetAttribute("href"));

                var activeGames = await _redisService.GetAsync("league_active:" + leagueUrl);

                var activeGamesSer = activeGames != null ? JsonSerializer.Deserialize<List<Game>>(activeGames): null;
                var thisGame = activeGamesSer != null ? activeGamesSer.FirstOrDefault(g => g.Id == thisGameId) : null;

                var thisAction = thisGame?.Actions.FirstOrDefault(a => a.Player.Name == player.Name && a.ActionType == gameAction.ActionType);

                var isPlayerParsed = thisAction != null && thisAction.Player.YellowCards != null;

                if (!isPlayerParsed)
                {
                    driver.Navigate().GoToUrl(player.Url);

                    player = await ParsePlayer(driver, player.Url, leagueName, thisGameId, leagueUrl);

                    if (!player.Empty)
                    {
                        switch (gameAction.ActionType)
                        {
                            case (GameActionType.RedCard):
                                player.RedCards++;
                                break;
                            case (GameActionType.YellowCard):
                                player.YellowCards++;
                                break;
                            case (GameActionType.YellowRedCard):
                                player.YellowRedCards++;
                                break;
                        }
                    }

                    player.Name = name;

                    driver.Navigate().Back();

                    try
                    {
                        await _redisService.SetAsync("player:" + player.Url, JsonSerializer.Serialize(player), TimeSpan.FromDays(2));
                        Console.WriteLine("SavedPlayer: " + player.Name);

                    }
                    catch
                    {
                        Console.WriteLine("Error SavedPlayer: " + player.Name);
                    }

                }
                else
                {
                    player = thisAction.Player;
                }

                gameAction.Player = player;
                gameAction.LeftTeam = leftTeam;

                actions.Add(gameAction);
            }

            for (var i = 0; i < playerElemsCount; i++)
            {
                var playerElem = driver.FindElements(By.CssSelector($".combined-lineups-container .container.{selector} tr"))[i];

                if (!playerElem.Text.Contains("for"))
                {
                    continue;
                }

                var checkTime = playerElem.FindElements(By.CssSelector(".substitute-out"));

                if (checkTime.Count < 1)
                {
                    continue;

                }

                var time = playerElem.FindElement(By.CssSelector(".substitute-out")).Text.Split(" ").Last();

                if (time.Length < 2 || _formatService.ToInt(time.Substring(0, 2)) > 45)
                {
                    continue;
                }

                var gameAction = new GameAction()
                {
                    ActionType = GameActionType.Switch
                };

                gameAction.Time = time;

                var linkElem = playerElem.FindElement(By.CssSelector(".substitute-in a"));
                var linkElem2 = playerElem.FindElement(By.CssSelector(".substitute-out a"));

                Player player = new();

                player.Name = _formatService.ClearString(linkElem2.Text);

                player.Url = linkElem2.GetAttribute("href");

                Player player2 = new();

                player2.Name = _formatService.ClearString(linkElem.Text);

                player2.Url = linkElem.GetAttribute("href");

                gameAction.Player = player;
                gameAction.Player2 = player2;

                gameAction.LeftTeam = leftTeam;

                actions.Add(gameAction);
            }

            return actions;
        }

        public async Task<Player> ParsePlayerByPage(string url, string leagueName)
        {
            var driver = new FirefoxDriver(_driverOptions);
            driver.Navigate().GoToUrl(url.Contains(_domain) ? url : _domain + url);

            var player = new Player();

            try
            {
                player = await ParsePlayer(driver, url, leagueName);
            }
            catch (Exception ex)
            {
                _errorsService.CreateErrorFile(ex);
            }
            finally
            {
                driver.Close();
                driver.Quit();
            }

            return player;
        }

        public async Task<Player> ParsePlayer(WebDriver driver, string url, string leagueName, string? thisGameId = null, string? leagueUrl = null)
        {
            Console.WriteLine($"Parse {url}. {leagueUrl}!");

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

            var haveName = driver.FindElements(By.CssSelector("h1")).Count > 0;

            var urlFinally = haveName ? url : url.ReplaceFirst("ru", "int");

            if (!haveName)
            {
                var intUrl = _formatService.ReplaceFirst(url, "ru", "int");
                driver.Navigate().GoToUrl(intUrl);

                var haveName2 = driver.FindElements(By.CssSelector("h1")).Count > 0;

                if (!haveName2)
                {
                    driver.Navigate().Back();

                    return new Player()
                    {
                        Name = null,
                        Position = null,
                        Url = urlFinally,
                        ImageUrl = null,
                        Goal = 0,
                        YellowCards = 0,
                        RedCards = 0,
                        YellowRedCards = 0,
                        GameCount = 0,
                        Empty = true
                    };
                }

                driver.Navigate().Back();
            }

            for (var i = 0; i < 4; i++)
            {
                driver.ExecuteScript("document.querySelector('.content ul li a')?.click()");
                wait.Until(d => d.FindElements(By.CssSelector(".block_player_career .overlay")).Count == 0);
            }

            var name = driver.FindElement(By.CssSelector("h1")).Text;
            var position = driver.FindElements(By.CssSelector("dd[data-position='position']")).Count > 0 ? driver.FindElement(By.CssSelector("dd[data-position='position']")).Text : null ;
            var imageUrl = driver.FindElements(By.CssSelector(".yui-u img")).Count > 0 ? driver.FindElement(By.CssSelector(".yui-u img")).GetAttribute("src") : null;

            Player player = new()
            {
                Name = _formatService.ClearString(name),
                Position = _formatService.ClearString(position),
                Url = urlFinally,
                ImageUrl = imageUrl,
                Goal = 0,
                YellowCards = 0,
                RedCards = 0,
                YellowRedCards = 0,
                GameCount = 0,
            };


            if (thisGameId != null && leagueUrl != null)
            {
                Console.WriteLine($"Parse {player.Name} by games!");
                var checkLeague = await _redisService.GetCachedDataByUrl(leagueUrl, DateTime.UtcNow.AddYears(-2));

                if (checkLeague != null)
                {

                    var checkGames = checkLeague.Games.Where(g => g.Id != thisGameId).ToList();

                    for (var i = 0; i < 30; i++)
                    {
                        var gamesItems = driver.FindElements(By.CssSelector("tr.match"));

                        var isHaveActual = gamesItems.Any(gameItem =>
                        {
                            var date = DateTime.UnixEpoch.AddSeconds(_formatService.ToInt(gameItem.GetAttribute("data-timestamp")));
                            var year = date.Year;
                            var actYear = DateTime.UtcNow.Year;
                            var actYear2 = DateTime.UtcNow.AddYears(-1).Year;

                            return year == actYear || year == actYear2;
                        });

                        if (!isHaveActual)
                        {
                            break;
                        }

                        if (isHaveActual)
                        {
                            foreach (var gameItem in gamesItems)
                            {
                                var id = gameItem.GetAttribute("data-event-id");

                                var inThisLeague = checkGames.Any(game => game.Id == id);

                                if (!inThisLeague)
                                {
                                    continue;
                                }

                                var gameItemHtml = gameItem.GetAttribute("innerHTML");

                                if (gameItemHtml.Contains("YC.png"))
                                {
                                    player.YellowCards++;
                                }
                                else if (gameItemHtml.Contains("Y2C.png"))
                                {
                                    player.YellowRedCards++;
                                }

                                if (gameItemHtml.Contains("RC.png"))
                                {
                                    player.RedCards++;
                                }

                                if (gameItemHtml.Contains("G.png"))
                                {
                                    player.Goal++;
                                }

                                player.GameCount++;

                            }
                        }

                        var prevBtnDis = driver.FindElements(By.CssSelector(".previous.disabled"));

                        if (prevBtnDis.Count > 0)
                        {
                            break;
                        }

                        var prevBtn = driver.FindElements(By.CssSelector(".previous"));

                        if(prevBtn.Count < 1)
                        {
                            break;
                        }

                        driver.ExecuteScript("document.querySelector('.previous')?.click()");
                        wait.Until(d => d.FindElements(By.CssSelector(".content .overlay")).Count == 0);
                    }
                }
            }
            else
            {
                Console.WriteLine($"Parse {player.Name} by table!");
                while (true)
                {
                    int addGoals = 0;
                    int addYellowCards = 0;
                    int addYellowRedCards = 0;
                    int addRedCards = 0;
                    int addGamesCount = 0;

                    try
                    {
                        var statContainer = driver.FindElement(By.CssSelector(".playerstats tbody"));

                        var statElems = statContainer.FindElements(By.CssSelector("tr"));

                        foreach (var statElem in statElems.Where(s =>
                        {
                            var competition = s.FindElement(By.CssSelector(".competition a")).GetAttribute("title");
                            var season = s.FindElement(By.CssSelector(".season a")).Text;
                            var yearNow = DateTime.UtcNow.AddHours(3).Year.ToString();
                            var foormattedSeason = _formatService.ClearString(season);

                            return _formatService.ClearString(competition).ToLower().Contains(leagueName.Split(" ")[0].ToLower()) && foormattedSeason.Contains(yearNow);

                        }).ToList())
                        {
                            var goals = statElem.FindElement(By.CssSelector("td.goals")).Text;
                            addGoals = _formatService.ToInt(goals);

                            var yellowCards = statElem.FindElement(By.CssSelector("td.yellow-cards")).Text;
                            addYellowCards = _formatService.ToInt(yellowCards);

                            var yellowRedCards = statElem.FindElement(By.XPath("td[contains(@class, '2nd-yellow-cards')]")).Text;
                            addYellowRedCards = _formatService.ToInt(yellowRedCards);

                            var redCards = statElem.FindElement(By.CssSelector("td.red-cards")).Text;
                            addRedCards = _formatService.ToInt(redCards);

                            var gamesCount = statElem.FindElement(By.CssSelector("td.appearances")).Text;
                            addGamesCount = _formatService.ToInt(gamesCount);
                        }

                        player.Goal = addGoals;
                        player.YellowCards = addYellowCards;
                        player.YellowRedCards = addYellowRedCards;
                        player.RedCards = addRedCards;
                        player.GameCount = addGamesCount;

                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error. Parse " + name + ". Try Again");
                    }
                }
            }

            Console.WriteLine($"Player {player.Name} \nData: \nY:{player.YellowCards}\nY2:{player.YellowRedCards}\nR:{player.RedCards}\nG:{player.Goal}");

            return player;
        }


        public async Task<List<Game>> GetActiveGamesByUrl(WebDriver driver, string url, string name, bool parseActions = true)
        {
            var games = new List<Game>();

            var leagueUrl = url.Contains(_domain) ? url : _domain + url;
            driver.Navigate().GoToUrl(leagueUrl);

            games = await GetGames(driver, name, leagueUrl, parseActions, true);


            return games;
        }
        public async Task<List<Game>> GetActiveGamesByUrl(string url, string name, bool parseActions = true)
        {

            var driver = new FirefoxDriver(_driverOptions);

            var games = new List<Game>();

            try
            {
                var leagueUrl = url.Contains(_domain) ? url : _domain + url;
                driver.Navigate().GoToUrl(leagueUrl);

                games = await GetGames(driver, leagueUrl, name, parseActions, true);
            }
            catch (Exception ex)
            {
                _errorsService.CreateErrorFile(ex);
            }
            finally
            {
                driver.Close();
                driver.Quit();
            }

            return games;
        }

        public async Task<List<Game>> GetGamesByUrl(string url, string name, DateTime? startDateFilter = null, DateTime? endDateFilter = null)
        {
            var driver = new FirefoxDriver(_driverOptions);

            var games = new List<Game>();

            try
            {
                var leagueUrl = url.Contains(_domain) ? url : _domain + url;
                driver.Navigate().GoToUrl(leagueUrl);
                games = await GetGames(driver, name, leagueUrl, false, false, startDateFilter, endDateFilter);
            }
            catch (Exception ex)
            {
                _errorsService.CreateErrorFile(ex);
            }
            finally
            {
                driver.Close();
                driver.Quit();
            }

            return games;
        }

        public async Task<Team[]> ParseTeams(WebDriver driver)
        {
            var team1Elem = driver.FindElement(By.CssSelector(".match-info .container.left"));
            var team2Elem = driver.FindElement(By.CssSelector(".match-info .container.right"));

            var countsElem = driver.FindElement(By.CssSelector(".match-info .container.middle")).Text;

            var counts = await ParseCounts(driver);

            Team team1 = new()
            {
                Name = team1Elem.FindElement(By.CssSelector(".team-title")).Text,
                IconUrl = team1Elem.FindElement(By.CssSelector(".team-logo img")).GetAttribute("src"),
                Count = counts[0]
            };

            Team team2 = new()
            {
                Name = team2Elem.FindElement(By.CssSelector(".team-title")).Text,
                IconUrl = team2Elem.FindElement(By.CssSelector(".team-logo img")).GetAttribute("src"),
                Count = counts[1]
            };

            Team[] teams = [team1, team2];

            return teams;
        }

        public async Task<Game> ParseGameByPage(string url, string leagueName)
        {
            var driver = new FirefoxDriver(_driverOptions);
            var game = new Game();

            try
            {
                driver.Navigate().GoToUrl(url.Contains(_domain) ? url : _domain + url);

                var dateElem = driver.FindElements(By.CssSelector(".details a"))[0];

                var dateTime = DateTime.ParseExact(dateElem.Text, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                var team1Elem = driver.FindElement(By.CssSelector(".match-info .container.left"));
                var team2Elem = driver.FindElement(By.CssSelector(".match-info .container.right"));

                var countsElem = driver.FindElement(By.CssSelector(".match-info .container.middle")).Text;

                var counts = await ParseCounts(driver);

                Team team1 = new()
                {
                    Name = team1Elem.FindElement(By.CssSelector(".team-title")).Text,
                    IconUrl = team1Elem.FindElement(By.CssSelector(".team-logo img")).GetAttribute("src"),
                    Count = counts[0]
                };

                Team team2 = new()
                {
                    Name = team2Elem.FindElement(By.CssSelector(".team-title")).Text,
                    IconUrl = team2Elem.FindElement(By.CssSelector(".team-logo img")).GetAttribute("src"),
                    Count = counts[1]
                };

                Team[] teams = [team1, team2];

                game = new Game()
                {
                    DateTime = dateTime,
                    Teams = teams,
                    Url = url,
                    GameTime = await ParseTime(driver),
                };
            }
            catch (Exception ex)
            {
                _errorsService.CreateErrorFile(ex);
            }

            driver.Close();
            driver.Quit();

            return game;
        }

        public async Task<LeagueIncludeGames> GetDataByUrl(string url, DateTime? startDateFilter = null, DateTime? endDateFilter = null)
        {
            var driver = new FirefoxDriver(_driverOptions);

            LeagueIncludeGames leagueIncludeGames = new();

            try
            {
                driver.Navigate().GoToUrl(url.Contains(_domain) ? url : _domain + url);

                var league = GetBaseLeagueInfo(driver);

                List<Game> games = (await GetGames(driver, league.Title, url, false, false, startDateFilter, endDateFilter));

                leagueIncludeGames = new()
                {
                    Title = league.Title,
                    Country = league.Country,
                    Url = url,
                    Games = games,
                };
            }
            catch (Exception ex)
            {
                _errorsService.CreateErrorFile(ex);
            }
            finally
            {
                driver.Close();
                driver.Quit();
            }

            return leagueIncludeGames;
        }

        public async Task<LeagueIncludeGames> GetDataByUrl(WebDriver driver, string url, DateTime? startDateFilter = null, DateTime? endDateFilter = null)
        {
            LeagueIncludeGames leagueIncludeGames = new();

            var leagueUrl = url.Contains(_domain) ? url : _domain + url;
            driver.Navigate().GoToUrl(leagueUrl);

            var league = GetBaseLeagueInfo(driver);

            List<Game> games = (await GetGames(driver, league.Title, leagueUrl, false, false, startDateFilter, endDateFilter));

            leagueIncludeGames = new()
            {
                Title = league.Title,
                Country = league.Country,
                Url = url,
                Games = games,
            };

            return leagueIncludeGames;
        }
    }
}
