using cardscore_api.Models;
using HtmlAgilityPack;
using System.Globalization;
using System.Text.RegularExpressions;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using cardscore_api.Data;
using System;
using System.Diagnostics.Metrics;

namespace cardscore_api.Services
{
    public class Soccer365ParserService
    {
        private readonly string _domain = "https://soccer365.ru/";
        private readonly int _timeFix = -2;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly HtmlWeb _web;
        private readonly DateTime _startDate = DateTime.UtcNow.AddDays(-7);

        private readonly FormatService _formatService;

        public Soccer365ParserService(IServiceScopeFactory scopeFactory, FormatService formatService)
        {
            _formatService = formatService;
            _scopeFactory = scopeFactory;
            _web = new HtmlWeb();
            _web.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.131 Safari/537.36";
        }
        public async Task<League> GetLeagueDataByUrl(string url)
        {
            var doc = _web.Load(url);

            int refetchCounter = 0;

            while (doc.ParsedText.Contains("503 Service Temporarily Unavailable") && refetchCounter < 20)
            {
                refetchCounter++;
                await Task.Delay(100);
                doc = _web.Load(url);
            }

            var h1Element = doc.DocumentNode.SelectSingleNode("//h1");

            var country = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'profile_params')]//tr[contains(., 'Страна')]//span");

            if (country == null)
            {
                country = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'profile_params')]//tr[contains(., 'Country')]//span");
            }

            var date = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'profile_params')]//tr[contains(., 'Дата')]//td[not(@class)]");

            if (date == null)
            {
                date = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'profile_params')]//tr[contains(., 'Date')]//td[not(@class)]");
            }

            var gamesCount = await GetLeagueGamesCount(url);

            League league = new()
            {
                Title = h1Element.InnerHtml + (country != null ? " " + country.InnerHtml : null!),
                Country = (country != null ? country.InnerHtml : null!),
                Url = url,
                GamesCount = gamesCount,
            };

            return league;
        }

        public async Task<int> GetLeagueGamesCount(string url)
        {
            var gameCountShedule = await GetPageGameCount(url + "shedule/");
            var gameCountResults = await GetPageGameCount(url + "results/");

            return gameCountShedule + gameCountResults;

        }
        public async Task<int> GetPageGameCount(string url)
        {
            var doc = _web.Load(url);

            int refetchCounter = 0;

            while (doc.ParsedText.Contains("503 Service Temporarily Unavailable") && refetchCounter < 20)
            {
                refetchCounter++;
                await Task.Delay(100);
                doc = _web.Load(url);
            }

            var gamesElems = doc.DocumentNode.SelectNodes("//div[@class='game_block ' or @class='game_block  online' or @class='game_block']");

            return gamesElems != null ? gamesElems.Count : 0;
        }

        public async Task<List<Game>> GetGamesByUrl(string url, string name, DateTime? startDateFilter = null, DateTime? endDateFilter = null)
        {
            List<Game> games = new();
            HashSet<string> gameUrls = new HashSet<string>();


            var results = await ParsePage(url + "results/", name, startDateFilter, endDateFilter);

            var shedule = new List<Game>();

            if (!(endDateFilter < DateTime.UtcNow.AddHours(_timeFix)))
            {
                shedule = await ParsePage(url + "shedule/", name, startDateFilter, endDateFilter);
            }

            results.Reverse();

            foreach (var game in results)
            {
                if (!gameUrls.Contains(game.Url))
                {
                    games.Add(game);
                    gameUrls.Add(game.Url);
                }
            }

            foreach (var game in shedule)
            {
                if (!gameUrls.Contains(game.Url))
                {
                    games.Add(game);
                    gameUrls.Add(game.Url);
                }
            }

            return games;
        }

        public async Task<List<Game>> GetActiveGamesByUrl(string url, string name, bool parseActions = true)
        {
            List<Game> games = new();

            var shedule = await ParsePageActive(url + "shedule/", name, parseActions);

            games.AddRange(shedule);

            return games;
        }

        public async Task<List<Game>> ParsePage(string url, string h1Element, DateTime? startDateFilter = null, DateTime? endDateFilter = null)
        {
            var doc = _web.Load(url);

            int refetchCounter = 0;

            while (doc.ParsedText.Contains("503 Service Temporarily Unavailable") && refetchCounter < 20)
            {
                refetchCounter++;
                await Task.Delay(100);
                doc = _web.Load(url);

            }
            var games = new Dictionary<string, Game>();

            var gameElems = doc.DocumentNode.SelectNodes("//div[@class='game_block ' or @class='game_block  online' or @class='game_block']");

            if (gameElems == null)
            {
                return [];
            }

            var gameElemsList = gameElems.ToList();

            foreach (var gameElem in gameElemsList)
            {
                Game game = null!;

                if (gameElem != null)
                {
                    game = await ParseGame(gameElem, h1Element, false, false, startDateFilter, endDateFilter);
                }

                if (game != null && !games.ContainsKey(game.Url))
                {
                    games[game.Url] = game;
                }

            }

            return games.Values.ToList();
        }


        public async Task<LeagueIncludeGames> GetDataByUrl(string url, DateTime? startDateFilter = null, DateTime? endDateFilter = null)
        {
            var doc = _web.Load(url);
            int refetchCounter = 0;

            while (doc.ParsedText.Contains("503 Service Temporarily Unavailable") && refetchCounter < 20)
            {
                refetchCounter++;
                await Task.Delay(100);
                doc = _web.Load(url);
            }

            var h1Element = doc.DocumentNode.SelectSingleNode("//h1");

            var country = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'profile_params')]//tr[contains(., 'Страна')]//span");

            if (country == null)
            {
                country = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'profile_params')]//tr[contains(., 'Country')]//span");
            }

            var date = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'profile_params')]//tr[contains(., 'Дата')]//td[not(@class)]");

            if (date == null)
            {
                date = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'profile_params')]//tr[contains(., 'Date')]//td[not(@class)]");
            }

            var dates = date != null ? date.InnerHtml.Split('-') : null!;

            List<Game> games = (await GetGamesByUrl(url, h1Element.InnerHtml, startDateFilter, endDateFilter));

            if (games != null)
            {
                games.Sort((x, y) => x.DateTime.CompareTo(y.DateTime));
            }

            LeagueIncludeGames league = new()
            {
                Title = h1Element.InnerHtml + (country != null ? " " + country.InnerHtml : ""),
                Country = country != null ? country.InnerHtml : null!,
                Url = url,
                Games = games,
            };

            return league;
        }

        public async Task<List<Game>> ParsePageActive(string url, string h1Element, bool parseActions = true)
        {
            var doc = _web.Load(url);
            int refetchCounter = 0;

            while (doc.ParsedText.Contains("503 Service Temporarily Unavailable") && refetchCounter < 20)
            {
                refetchCounter++;
                await Task.Delay(100);
                doc = _web.Load(url);
            }

            var games = new Dictionary<string, Game>();

            var gameElems = doc.DocumentNode.SelectNodes("//div[@class='game_block ' or @class='game_block  online' or @class='game_block']");

            if (gameElems == null)
            {
                return games.Values.ToList();
            }

            if (gameElems == null)
            {
                return [];
            }

            var gameElemsList = gameElems.ToList();

            foreach (var gameElem in gameElemsList)
            {
                Game? game = null!;

                if (gameElem != null)
                {
                    game = await ParseGame(gameElem, h1Element, parseActions, true);
                }

                if (game != null)
                {
                    if (!games.ContainsKey(game.Url))
                    {
                        games[game.Url] = game;
                    }
                }
            }

            return games.Values.ToList();
        }
        public async Task<Player> ParsePlayerByPage(string url, string leagueName)
        {
            string tabLastGames = "&tab=last_games";

            var tabPlayerDoc = _web.Load(url + tabLastGames);

            int tabPlayerDocRefetchCounter = 0;

            while (tabPlayerDoc.ParsedText.Contains("503 Service Temporarily Unavailable") && tabPlayerDocRefetchCounter < 20)
            {
                tabPlayerDocRefetchCounter++;
                await Task.Delay(100);
                tabPlayerDoc = _web.Load(url + tabLastGames);
            }

            var pagesCounter = tabPlayerDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'pager')]");

            var counts = pagesCounter != null ? pagesCounter.SelectNodes(".//a")?.Count : 1;

            var player = await ParsePlayerMainInfo(url, leagueName);

            player.Goal = 0;
            player.Assists = 0;
            player.YellowCards = 0;
            player.YellowRedCards = 0;
            player.RedCards = 0;
            player.GameCount = 0;

            for (var i = 1; i <= counts; i++)
            {
                using (var _dataContext = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<DataContext>())
                {

                    var league = _dataContext.Leagues.FirstOrDefault(l => l.Title == leagueName);

                    var thisUrl = url + tabLastGames + $"&p={i.ToString()}";

                    var tabPlayerDocPage = _web.Load(thisUrl);

                    int refetchCounter = 0;
                    while (tabPlayerDocPage.ParsedText.Contains("503 Service Temporarily Unavailable") && refetchCounter < 20)
                    {
                        refetchCounter++;
                        await Task.Delay(100);
                        tabPlayerDocPage = _web.Load(thisUrl);
                    }

                    var table = tabPlayerDocPage.DocumentNode.SelectSingleNode("//table[contains(@class, 'tablesorter')]");

                    if (!table.InnerHtml.Contains(DateTime.UtcNow.AddHours(_timeFix).Year.ToString()))
                    {
                        break;
                    }

                    var nameForPlayerPage = string.Join(" ", _formatService.ClearString(leagueName).Split(" ")[0..^1]);

                    string selector = nameForPlayerPage.Contains('\'') ? nameForPlayerPage.Split('\'')[1] : nameForPlayerPage;

                    var tableThisLeagueItems = table.SelectNodes($".//tr[contains(., '{selector}')]");

                    if (tableThisLeagueItems == null || tableThisLeagueItems.Count < 1)
                    {
                        continue;
                    }

                    foreach (var tableThisLeagueItem in tableThisLeagueItems)
                    {
                        if (tableThisLeagueItem.InnerHtml.Contains("Запасные игроки"))
                        {
                            continue;
                        }

                        var dateElem = tableThisLeagueItem.SelectSingleNode(".//td");

                        DateTime date = _formatService.ParseDateTimeDDMMYYYY(_formatService.ClearString(dateElem.LastChild.InnerHtml));

                        if (league.StartDate > date || date > league.EndDate.AddHours(24))
                        {
                            continue;
                        }

                        player.GameCount++;

                        var playerGoalsElem = tableThisLeagueItem.SelectSingleNode(".//td[5]");
                        var playerAssistsElem = tableThisLeagueItem.SelectSingleNode(".//td[6]");
                        var playerYellowCardsElem = tableThisLeagueItem.SelectSingleNode(".//td[7]");
                        var playerRedCardsElem = tableThisLeagueItem.SelectSingleNode(".//td[9]");
                        var playerYellowRedCardsElem = tableThisLeagueItem.SelectSingleNode(".//td[8]");

                        var isYellowCardExist = (playerYellowCardsElem != null && _formatService.ClearString(playerYellowCardsElem.InnerText) != "&nbsp;");
                        var isYellowRedCardExist = (playerYellowRedCardsElem != null && _formatService.ClearString(playerYellowRedCardsElem.InnerText) != "&nbsp;");

                        if (isYellowRedCardExist) player.YellowRedCards++;
                        else if (isYellowCardExist) player.YellowCards++;

                        if (playerGoalsElem != null) player.Goal += _formatService.ToInt(playerGoalsElem.InnerText);
                        if (playerAssistsElem != null) player.Assists += _formatService.ToInt(playerAssistsElem.InnerText);
                        if (playerRedCardsElem != null && _formatService.ClearString(playerRedCardsElem.InnerText) != "&nbsp;") player.RedCards++;
                    }
                }
            }

            return player;
        }

        public async Task<Player> ParsePlayerMainInfo(string url, string leagueName)
        {
            var player = new Player()
            {
                Url = url,
            };

            var playerDoc = _web.Load(url);

            int refetchCounter = 0;
            while (playerDoc.ParsedText.Contains("503 Service Temporarily Unavailable") && refetchCounter < 20)
            {
                refetchCounter++;
                await Task.Delay(100);
                playerDoc = _web.Load(url);
            }


            var nameForPlayerPage = string.Join(" ", _formatService.ClearString(leagueName).Split(" ")[0..^1]);

            string selector = nameForPlayerPage.Contains('\'') ? nameForPlayerPage.Split('\'')[1] : nameForPlayerPage;

            var thisLeagueElem = playerDoc.DocumentNode.SelectSingleNode($"//div[contains(@class, 'pl_stat_block')]//tr[contains(., '{selector}')]");

            var playerImageElem = playerDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'profile_foto')]//img");

            var playerNameElem = playerDoc.DocumentNode.SelectSingleNode("//h1");

            var playerPosElem = playerDoc.DocumentNode.SelectSingleNode("//tr[contains(., 'Позиция')]//td[not(@class)]");

            player.Position = playerPosElem != null ? _formatService.ClearString(playerPosElem.InnerHtml) : null;
            player.ImageUrl = playerImageElem != null ? playerImageElem.Attributes["src"].Value : null!;
            player.Name = playerNameElem != null ? _formatService.ClearString(playerNameElem.InnerText) : null;

            return player;

        }

        public async Task<Player> ParsePlayer(string url, string leagueName)
        {
            var player = new Player()
            {
                Url = url,
            };

            var playerDoc = _web.Load(url);

            int refetchCounter = 0;

            while (playerDoc.ParsedText.Contains("503 Service Temporarily Unavailable") && refetchCounter < 20)
            {
                refetchCounter++;
                await Task.Delay(100);
                playerDoc = _web.Load(url);
            }

            var nameForPlayerPage = string.Join(" ", _formatService.ClearString(leagueName).Split(" ")[0..^1]);

            string selector = nameForPlayerPage.Contains('\'') ? nameForPlayerPage.Split('\'')[1] : nameForPlayerPage;

            var thisLeagueElem = playerDoc.DocumentNode.SelectSingleNode($"//div[contains(@class, 'pl_stat_block')]//tr[contains(., '{selector}')]");

            var playerImageElem = playerDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'profile_foto')]//img");

            var playerNameElem = playerDoc.DocumentNode.SelectSingleNode("//h1");

            if (thisLeagueElem != null)
            {
                var playerGoalsElem = thisLeagueElem.SelectSingleNode(".//td[contains(@class, 'al_c')][2]");
                var playerAssistsElem = thisLeagueElem.SelectSingleNode(".//td[contains(@class, 'al_c')][4]");
                var playerYellowCardsElem = thisLeagueElem.SelectSingleNode(".//td[contains(@class, 'al_c')][9]");
                var playerRedCardsElem = thisLeagueElem.SelectSingleNode(".//td[contains(@class, 'al_c')][11]");
                var playerYellowRedCardsElem = thisLeagueElem.SelectSingleNode(".//td[contains(@class, 'al_c')][10]");
                var playerGameCountElem = thisLeagueElem.SelectSingleNode(".//td[contains(@class, 'al_c')][1]");

                player.Goal = playerGoalsElem != null ? _formatService.ToInt(playerGoalsElem.InnerHtml) : null;
                player.Assists = playerAssistsElem != null ? _formatService.ToInt(playerAssistsElem.InnerHtml) : null;
                player.YellowCards = playerYellowCardsElem != null ? _formatService.ToInt(playerYellowCardsElem.InnerHtml) : null;
                player.RedCards = playerRedCardsElem != null ? _formatService.ToInt(playerRedCardsElem.InnerHtml) : null;
                player.YellowRedCards = playerYellowRedCardsElem != null ? _formatService.ToInt(playerYellowRedCardsElem.InnerHtml) : null;
                player.GameCount = playerGameCountElem != null ? _formatService.ToInt(playerGameCountElem.InnerHtml) : null;
            }

            var playerPosElem = playerDoc.DocumentNode.SelectSingleNode("//tr[contains(., 'Позиция')]//td[not(@class)]");

            if (playerPosElem == null)
            {
                playerPosElem = playerDoc.DocumentNode.SelectSingleNode("//tr[contains(., 'Position')]//td[not(@class)]");
            }

            player.Position = playerPosElem != null ? _formatService.ClearString(playerPosElem.InnerHtml) : null;
            player.ImageUrl = playerImageElem != null ? playerImageElem.Attributes["src"].Value : null!;
            player.Name = playerNameElem != null ? _formatService.ClearString(playerNameElem.InnerText) : null;

            return player;
        }

        public async Task<Game> ParseGameByPage(string url, string leagueName, bool withActions = true)
        {
            var gameDoc = _web.Load(url);

            int refetchCounter = 0;

            while (gameDoc.ParsedText.Contains("503 Service Temporarily Unavailable") && refetchCounter < 20)
            {
                refetchCounter++;
                await Task.Delay(100);
                gameDoc = _web.Load(url);
            }

            var dateTime = await ParseDateFromPage(gameDoc);

            var team1Elem = gameDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'live_game left')]");
            HtmlNode team1Name = null!;
            HtmlNode team1IconUrl = null!;
            HtmlNode team1Count = null!;

            if (team1Elem != null)
            {
                team1Name = team1Elem.SelectSingleNode(".//div[@class='live_game_ht']");
                team1IconUrl = team1Elem.SelectSingleNode(".//img");
                team1Count = team1Elem.SelectSingleNode(".//div[@class='live_game_goal']");
            }

            var team2Elem = gameDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'live_game right')]");
            HtmlNode team2Name = null!;
            HtmlNode team2IconUrl = null!;
            HtmlNode team2Count = null!;

            if (team2Elem != null)
            {
                team2Name = team2Elem.SelectSingleNode(".//div[@class='live_game_at']");
                team2IconUrl = team2Elem.SelectSingleNode(".//img");
                team2Count = team2Elem.SelectSingleNode(".//div[@class='live_game_goal']");
            }

            Team team1 = new()
            {
                Name = team1Name != null ? _formatService.ClearString(team1Name.InnerText) : null,
                IconUrl = team1IconUrl != null ? team1IconUrl.Attributes["src"].Value : null,
                Count = team1Count != null ? _formatService.ToInt(team1Count.InnerHtml) : null,
            };
            Team team2 = new()
            {
                Name = team2Name != null ? _formatService.ClearString(team2Name.InnerText) : null,
                IconUrl = team2IconUrl != null ? team2IconUrl.Attributes["src"].Value : null,
                Count = team2Count != null ? _formatService.ToInt(team2Count.InnerHtml) : null,
            };

            Team[] teams = new[]
            {
                team1, team2
            };

            Game game = new()
            {
                Teams = teams,

                DateTime = dateTime,

                Url = url
            };

            if (!withActions)
            {
                return game;
            }

            var liveStatusElem = gameDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'live_game_status')]/b");

            if (liveStatusElem != null && (liveStatusElem.InnerHtml.Contains("мин") || liveStatusElem.InnerHtml.Contains("Перерыв") || liveStatusElem.InnerHtml.Contains("Break")))
            {
                game.ActiveGame = true;
                game.GameTime = _formatService.ClearString(liveStatusElem.InnerHtml);
            }

            else if (liveStatusElem != null && (liveStatusElem.InnerHtml.Contains("Завершен") || liveStatusElem.InnerHtml.Contains("Finished")))
            {
                game.FinishedToday = true;
            }

            var actionsElems = gameDoc.DocumentNode.SelectNodes("//div[contains(@class, 'block_body_nopadding') and contains(@class, 'padv15')]/div[not(@class)]");

            if (actionsElems != null)
            {
                var actions = await ParseActions(actionsElems, leagueName, gameDoc, teams, game.ActiveGame);
                game.Actions = actions;
                game.ActionsCount = actions != null ? actions.Count : 0;
            }

            return game;
        }

        public async Task<List<GameAction>> ParseSwitches(HtmlDocument gameDoc, string leagueName, string url = "")
        {
            List<GameAction> actions = new List<GameAction>();

            return actions;
        }

        public async Task<List<GameAction>> ParseActions(HtmlNodeCollection actionsElems, string leagueName, HtmlDocument gameDoc, Team[] teams, bool isActive)
        {

            List<GameAction> actions = new();

            if (actionsElems != null)
            {
                foreach (var actionElem in actionsElems.ToList())
                {
                    var eventAt = actionElem.SelectSingleNode(".//div[contains(@class, 'event_at')]");
                    bool leftTeam = eventAt != null ? _formatService.ClearString(eventAt.InnerText) == "" : false;

                    var actionIconElem = leftTeam ? actionElem.SelectSingleNode(".//div[contains(@class, 'event_ht_icon')]") : actionElem.SelectSingleNode(".//div[contains(@class, 'event_at_icon')]");
                    var actionIcon = actionIconElem != null ? actionIconElem.GetClasses().ToList()[1] : null;
                    GameActionType? actionType = null;

                    switch (actionIcon)
                    {
                        case "live_yellowcard":
                            actionType = GameActionType.YellowCard; break;
                        case "live_redcard":
                            actionType = GameActionType.RedCard; break;
                        case "live_yellowred":
                            actionType = GameActionType.YellowRedCard; break;
                    }

                    if (actionType == null)
                    {
                        continue;
                    }

                    var actionTime = actionElem.SelectSingleNode(".//div[contains(@class, 'event_min')]");

                    var playerElem = actionElem.SelectSingleNode(".//a");


                    string playerLink = null!;


                    if (playerElem == null)
                    {
                        playerElem = leftTeam ? actionElem.SelectSingleNode(".//div[contains(@class, 'event_ht')]") : actionElem.SelectSingleNode(".//div[contains(@class, 'event_at')]");
                    }

                    else
                    {
                        playerLink = playerElem != null ? playerElem.Attributes["href"].Value : null;
                    }

                    var player = new Player();

                    player.Name = playerElem != null ? _formatService.ClearString(playerElem.InnerText) : null;

                    if (player.Name == "нет данных")
                    {
                        continue;
                    }

                    player.Url = playerLink;

                    if (playerLink != null)
                    {
                        if (isActive)
                        {
                            player = await ParsePlayerByPage(_domain + playerLink, leagueName);
                        }
                        else
                        {
                            player = await ParsePlayer(_domain + playerLink, leagueName);
                        }
                    }
                    else
                    {
                        string selector = player.Name.Contains('\'') ? player.Name.Split('\'')[1] : player.Name;
                        var lineUpPlayerElem = gameDoc.GetElementbyId("lineups").SelectSingleNode($".//a[contains(., '{selector}')]");

                        if (lineUpPlayerElem != null)
                        {
                            player.Url = lineUpPlayerElem.Attributes["href"].Value;

                            if (isActive)
                            {
                                player = await ParsePlayerByPage(_domain + player.Url, leagueName);
                            }
                            else
                            {
                                player = await ParsePlayer(_domain + player.Url, leagueName);
                            }
                        }
                    }

                    var actionView = new GameAction()
                    {
                        Time = actionTime != null ? _formatService.ClearString(actionTime.InnerHtml) : null,
                        LeftTeam = leftTeam,
                        ActionType = actionType != null ? actionType : null,
                        Player = player,
                    };

                    var isDublicate = actions.Find(a => a.Player.Name == actionView.Player.Name && a.ActionType == actionView.ActionType);

                    if (isDublicate == null)
                    {
                        actions.Add(actionView);
                    }
                }
            };

            var switches = await ParseSwitches(gameDoc, leagueName);

            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                var player = action.Player;

                if (player.Url == null)
                {
                    continue;
                }

                var playerActions = actions.Where(x => x.Player.Name == player.Name).ToList();

                var checkedPlayer = await CheckActionsByPlayerPage(playerActions, player, leagueName, teams);

                actions[i].Player = checkedPlayer;
            }

            actions.AddRange(switches);

            actions.Sort((a, b) => _formatService.ToInt(a.Time).CompareTo(_formatService.ToInt(b.Time)));

            return actions;
        }

        public async Task<Player> CheckActionsByPlayerPage(List<GameAction> gameActions, Player player, string leagueName, Team[] teams)
        {
            var newPlayer = new Player()
            {
                Name = player.Name,
                Url = player.Url,
                YellowCards = player.YellowCards,
                ImageUrl = player.ImageUrl,
                GameCount = player.GameCount,
                Goal = player.Goal,
                Assists = player.Assists,
                RedCards = player.RedCards,
                YellowRedCards = player.YellowRedCards,
                Position = player.Position,
            };

            string tabLastGames = "&tab=last_games";

            var tabPlayerDoc = _web.Load(player.Url + tabLastGames);

            int refetchCounter = 0;

            while (tabPlayerDoc.ParsedText.Contains("503 Service Temporarily Unavailable") && refetchCounter < 20)
            {
                refetchCounter++;
                await Task.Delay(100);
                tabPlayerDoc = _web.Load(player.Url + tabLastGames);
            }

            var table = tabPlayerDoc.DocumentNode.SelectSingleNode("//table[contains(@class, 'tablesorter')]");

            var nameForPlayerPage = string.Join(" ", _formatService.ClearString(leagueName).Split(" ")[0..^1]);

            string selector = nameForPlayerPage.Contains('\'') ? nameForPlayerPage.Split('\'')[1] : nameForPlayerPage;

            var tableThisGameItem = table.SelectSingleNode($".//tr[contains(., '{selector}') and contains(., '{teams[0].Name}') and contains(., '{teams[1].Name}')]");

            if (tableThisGameItem == null)
            {
                foreach (var action in gameActions)
                {
                    switch (action.ActionType)
                    {
                        case (GameActionType.YellowCard):
                            newPlayer.YellowCards++;
                            break;
                        case (GameActionType.YellowRedCard):
                            newPlayer.YellowRedCards++;
                            newPlayer.YellowCards--;
                            break;
                        case (GameActionType.RedCard):
                            newPlayer.RedCards++;
                            break;
                    }
                }
            }
            else
            {
                var playerYellowCardsElem = tableThisGameItem.SelectSingleNode(".//td[7]");
                var playerRedCardsElem = tableThisGameItem.SelectSingleNode(".//td[9]");
                var playerYellowRedCardsElem = tableThisGameItem.SelectSingleNode(".//td[8]");

                var isYellowCardExist = (playerYellowCardsElem != null && _formatService.ClearString(playerYellowCardsElem.InnerText) != "&nbsp;");
                var isYellowRedCardExist = (playerYellowRedCardsElem != null && _formatService.ClearString(playerYellowRedCardsElem.InnerText) != "&nbsp;");
                var isRedCardExist = (playerRedCardsElem != null && _formatService.ClearString(playerRedCardsElem.InnerText) != "&nbsp;");


                foreach (var action in gameActions)
                {
                    switch (action.ActionType)
                    {
                        case (GameActionType.YellowCard):
                            if (!isYellowCardExist) newPlayer.YellowCards++;
                            break;
                        case (GameActionType.YellowRedCard):
                            if (!isYellowRedCardExist)
                            {
                                newPlayer.YellowRedCards++;
                                newPlayer.YellowCards--;
                            }
                            break;
                        case (GameActionType.RedCard):
                            if (!isRedCardExist) newPlayer.RedCards++;
                            break;
                    }
                }
            }

            return newPlayer;
        }

        public async Task<Game> ParseGame(HtmlNode htmlNode, string leagueName, bool parseActions = true, bool onlyActive = false, DateTime? startDateFilter = null, DateTime? endDateFilter = null)
        {
            var dateElem = htmlNode.SelectSingleNode(".//div[contains(@class, 'status')]");

            if (onlyActive && dateElem != null && (!dateElem.InnerHtml.Contains("'") && !dateElem.InnerHtml.Contains("Перерыв") && !dateElem.InnerHtml.Contains("Break")))
            {
                return null!;
            }

            string url = htmlNode.SelectSingleNode(".//a").Attributes["href"].Value;

            DateTime dateTime = dateElem != null && _formatService.CanParseDDMM(_formatService.ClearString(dateElem.InnerHtml)) ? _formatService.ParseDateTimeDDMM(_formatService.ClearString(dateElem.InnerHtml)).AddHours(_timeFix) :
                _formatService.CanParseDDMMYY(_formatService.ClearString(dateElem.InnerHtml)) ? _formatService.ParseDateTimeDDMMYY(_formatService.ClearString(dateElem.InnerHtml)).AddHours(_timeFix) :
                startDateFilter != null || endDateFilter != null ? DateTime.Today.AddHours(1) :
                await ParseDateFromPage(_domain + url);

            if ((dateTime < startDateFilter) || (dateTime > endDateFilter))
            {
                return null!;
            }

            if ((startDateFilter != null || endDateFilter != null) && dateTime == DateTime.Today.AddHours(1))
            {
                dateTime = await ParseDateFromPage(_domain + url);
            }

            var team1Elem = htmlNode.SelectSingleNode(".//div[contains(@class, 'result')]//div[contains(@class, 'ht')]");
            HtmlNode team1Name = null!;
            HtmlNode team1IconUrl = null!;
            HtmlNode team1Count = null!;

            if (team1Elem != null)
            {
                team1Name = team1Elem.SelectSingleNode(".//div[@class='name']");
                team1IconUrl = team1Elem.SelectSingleNode(".//img");
                team1Count = team1Elem.SelectSingleNode(".//div[@class='gls']");
            }

            var team2Elem = htmlNode.SelectSingleNode(".//div[contains(@class, 'result')]//div[contains(@class, 'at')]");
            HtmlNode team2Name = null!;
            HtmlNode team2IconUrl = null!;
            HtmlNode team2Count = null!;

            if (team2Elem != null)
            {
                team2Name = team2Elem.SelectSingleNode(".//div[@class='name']");
                team2IconUrl = team2Elem.SelectSingleNode(".//img");
                team2Count = team2Elem.SelectSingleNode(".//div[@class='gls']");
            }

            Team team1 = new()
            {
                Name = team1Name != null ? _formatService.ClearString(team1Name.InnerText) : null,
                IconUrl = team1IconUrl != null ? team1IconUrl.Attributes["src"].Value : null,
                Count = team1Count != null ? _formatService.ToInt(team1Count.InnerHtml) : null,
            };
            Team team2 = new()
            {
                Name = team2Name != null ? _formatService.ClearString(team2Name.InnerText) : null,
                IconUrl = team2IconUrl != null ? team2IconUrl.Attributes["src"].Value : null,
                Count = team2Count != null ? _formatService.ToInt(team2Count.InnerHtml) : null,
            };

            Team[] teams = new[]
            {
                team1, team2
            };

            Game game = new()
            {
                Teams = teams,

                DateTime = await _formatService.DateTimeRounding(dateTime),

                Url = _domain + url
            };

            if (dateElem != null && dateElem.InnerHtml.Contains("'"))
            {
                game.ActiveGame = true;
                game.GameTime = _formatService.ClearString(dateElem.InnerHtml);
            }

            else if (dateElem != null && Regex.IsMatch(_formatService.ClearString(dateElem.InnerHtml), @"^\d\d:\d\d$"))
            {
                game.IsToday = true;
                game.GameTime = _formatService.SubtractTime(_formatService.ClearString(dateElem.InnerHtml), -_timeFix);
            }

            else if (dateElem != null && (dateElem.InnerHtml.Contains("Завершен") || dateElem.InnerHtml.Contains("Finished")))
            {
                game.FinishedToday = true;
            }

            else if (dateElem != null && (dateElem.InnerHtml.Contains("Перерыв") || dateElem.InnerHtml.Contains("Break")))
            {
                game.ActiveGame = true;
                game.GameTime = _formatService.ClearString(dateElem.InnerHtml);
            }

            else if (dateElem != null && (dateElem.InnerHtml.Contains("Перенесен") || dateElem.InnerHtml.Contains("Postponed")))
            {
                game.DateTime = _formatService.ParseDateTimeDDMM(_formatService.ClearString(dateElem.SelectSingleNode(".//span").InnerHtml)).AddHours(_timeFix);
            }

            else if (dateElem != null && (dateElem.InnerHtml.Contains("Остановлен") || dateElem.InnerHtml.Contains("Stopped")))
            {
                game.IsStopped = true;
                game.GameTime = _formatService.ClearString(dateElem.InnerHtml);
            }

            if (parseActions && url != null)
            {



                var gameDoc = _web.Load(_domain + url);

                int refetchCounter = 0;

                while (gameDoc.ParsedText.Contains("503 Service Temporarily Unavailable") && refetchCounter < 20)
                {
                    refetchCounter++;
                    await Task.Delay(100);
                    gameDoc = _web.Load(_domain + url);
                }

                var actionsElems = gameDoc.DocumentNode.SelectNodes("//div[contains(@class, 'block_body_nopadding') and contains(@class, 'padv15')]/div[not(@class)]");

                var actions = await ParseActions(actionsElems, leagueName, gameDoc, teams, game.ActiveGame);
                game.Actions = actions;
                game.ActionsCount = actions != null ? actions.Count : 0;
            }

            return game;
        }

        public async Task<DateTime> ParseDateFromPage(HtmlDocument gameDoc)
        {
            var dateElem = gameDoc.DocumentNode.SelectSingleNode("//h2");

            DateTime dateTime = DateTime.UtcNow.AddHours(3);

            var dateString = dateElem != null ? _formatService.ClearString(dateElem.InnerHtml).Split(", ").Last() : null;

            if (dateString != null && !dateString.Contains(":"))
            {
                var liveGameElem = gameDoc.DocumentNode.SelectSingleNode("//div[contains(@class,'live_game_status')]");

                if (liveGameElem != null)
                {
                    dateTime = DateTime.UtcNow.AddHours(3).AddMinutes(-_formatService.ToInt(liveGameElem.InnerHtml));
                }

            }
            else
            {
                if (dateString != null)
                {
                    dateTime = _formatService.TryParseDateTime(dateString) ? _formatService.ParseDateTime(_formatService.ClearString(dateString)).AddHours(_timeFix) : DateTime.UtcNow.AddHours(3);
                }
            }


            return await _formatService.DateTimeRounding(dateTime);
        }

        public async Task<DateTime> ParseDateFromPage(string url)
        {

            var gameDoc = _web.Load(url);

            int refetchCounter = 0;

            while (gameDoc.ParsedText.Contains("503 Service Temporarily Unavailable") && refetchCounter < 20)
            {
                refetchCounter++;
                await Task.Delay(100);
                gameDoc = _web.Load(url);
            }

            var dateElem = gameDoc.DocumentNode.SelectSingleNode("//h2");

            DateTime dateTime = DateTime.UtcNow.AddHours(3);

            var dateString = dateElem != null ? _formatService.ClearString(dateElem.InnerHtml).Split(", ").Last() : null;

            if (dateString != null && !dateString.Contains(":"))
            {
                var liveGameElem = gameDoc.DocumentNode.SelectSingleNode("//div[contains(@class,'live_game_status')]");

                if (liveGameElem != null)
                {
                    dateTime = DateTime.UtcNow.AddHours(3).AddMinutes(-_formatService.ToInt(liveGameElem.InnerHtml));
                }

            }
            else
            {
                if (dateString != null)
                {
                    dateTime = _formatService.TryParseDateTime(dateString) ? _formatService.ParseDateTime(_formatService.ClearString(dateString)).AddHours(_timeFix) : DateTime.UtcNow.AddHours(3);
                }
            }


            return await _formatService.DateTimeRounding(dateTime);
        }

    }
}
