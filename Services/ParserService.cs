using cardscore_api.Data;
using cardscore_api.Models;
using cardscore_api.Services.ParserServices;
using OpenQA.Selenium;
using OpenQA.Selenium.Internal;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace cardscore_api.Services
{
    public class ParserService
    {
        private readonly Soccer365ParserService _soccer365parserService;
        private readonly SoccerwayParserService _soccerwayParserService;
        private readonly LeagueParseListService _leagueParseListService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly RedisService _redisService;

        private readonly DateTime _startDate;
        public ParserService(Soccer365ParserService soccer365ParserService, LeagueParseListService leagueParseListService, SoccerwayParserService soccerwayParserService, IServiceScopeFactory scopeFactory, RedisService redisService)
        {
            _soccer365parserService = soccer365ParserService;
            _leagueParseListService = leagueParseListService;
            _soccerwayParserService = soccerwayParserService;
            _scopeFactory = scopeFactory;
            _redisService = redisService;

            _startDate = DateTime.UtcNow.AddDays(-4);
        }

        public async Task<List<Game>> GetGamesByUrl(string url, string leagueName, DateTime? startDate = null!, DateTime? endDate = null!)
        {
            if (startDate == null)
            {
                startDate = _startDate;
            }

            List<Game> games = new();

            var leagueParseData = await _leagueParseListService.GetByUrl(url);

            if (leagueParseData != null)
            {
                switch ((ParserType)leagueParseData.ParserType)
                {
                    case ParserType.Soccer365:
                        games = await _soccer365parserService.GetGamesByUrl(url, leagueName, startDate);
                        break;
                    case ParserType.Soccerway:
                        games = await _soccerwayParserService.GetGamesByUrl(url, leagueName, startDate);
                        break;
                }
            }
            else
            {
                games = await _soccerwayParserService.GetGamesByUrl(url, leagueName, startDate);
            }

            return games;
        }

        public async Task<LeagueIncludeGames> GetDataByUrl(string url, DateTime? startDate = null!, DateTime? endDate = null!)
        {
            if (startDate == null)
            {
                startDate = _startDate;
            }

            LeagueIncludeGames data = new();

            var leagueParseData = await _leagueParseListService.GetByUrl(url);

            if (leagueParseData != null)
            {
                switch ((ParserType)leagueParseData.ParserType)
                {
                    case ParserType.Soccer365:
                        data = await _soccer365parserService.GetDataByUrl(url, startDate, endDate);
                        break;
                    case ParserType.Soccerway:
                        data = await _soccerwayParserService.GetDataByUrl(url, startDate, endDate);
                        break;
                }
            }
            else
            {
                data = await _soccerwayParserService.GetDataByUrl(url, startDate, endDate);
            }

            return data;
        }

        public async Task<LeagueIncludeGames> GetDataByUrl(WebDriver driver, string url, DateTime? startDate = null!, DateTime? endDate = null!)
        {
            if (startDate == null)
            {
                startDate = _startDate;
            }

            LeagueIncludeGames data = new();

            var leagueParseData = await _leagueParseListService.GetByUrl(url);

            if (leagueParseData != null)
            {
                switch ((ParserType)leagueParseData.ParserType)
                {
                    case ParserType.Soccer365:
                        data = await _soccer365parserService.GetDataByUrl(url, startDate, endDate);
                        break;
                    case ParserType.Soccerway:
                        data = await _soccerwayParserService.GetDataByUrl(driver, url, startDate, endDate);
                        break;
                }
            }
            else
            {
                data = await _soccerwayParserService.GetDataByUrl(driver, url, startDate, endDate);
            }

            return data;
        }

        public async Task<Game> ParseGameByPage(string url, string leagueName, bool withActions = false)
        {
            Game game = new();

            var leagueParseData = await _leagueParseListService.GetByUrl(url);

            if (leagueParseData != null)
            {
                switch ((ParserType)leagueParseData.ParserType)
                {
                    case ParserType.Soccer365:
                        game = await _soccer365parserService.ParseGameByPage(url, leagueName, withActions);
                        break;
                    case ParserType.Soccerway:
                        game = await _soccerwayParserService.ParseGameByPage(url, leagueName);
                        break;
                }
            }
            else
            {
                game = await _soccerwayParserService.ParseGameByPage(url, leagueName);
            }

            return game;
        }

        public async Task<Player> ParsePlayerByPage(string url, string leagueName)
        {
            Player player = new();

            var leagueParseData = await _leagueParseListService.GetByUrl(url);

            if (leagueParseData != null)
            {
                switch ((ParserType)leagueParseData.ParserType)
                {
                    case ParserType.Soccer365:
                        player = await _soccer365parserService.ParsePlayerByPage(url, leagueName);
                        break;
                    case ParserType.Soccerway:
                        player = await _soccerwayParserService.ParsePlayerByPage(url, leagueName);
                        break;
                }
            }
            else
            {
                player = await _soccerwayParserService.ParsePlayerByPage(url, leagueName);
            }

            return player;
        }

        public async Task<League> GetLeagueDataByUrl(string url)
        {
            League league = new();

            var leagueParseData = await _leagueParseListService.GetByUrl(url);

            if (leagueParseData != null)
            {
                switch ((ParserType)leagueParseData.ParserType)
                {
                    case ParserType.Soccer365:
                        league = await _soccer365parserService.GetLeagueDataByUrl(url);
                        break;
                    case ParserType.Soccerway:
                        league = await _soccerwayParserService.GetLeagueDataByUrl(url);
                        break;
                }
            }
            else
            {
                league = await _soccerwayParserService.GetLeagueDataByUrl(url);
            }

            return league;
        }

        public async Task<League> GetLeagueDataByUrl(ParserType parserType, string url)
        {
            League league = new();

            switch ((ParserType)parserType)
            {
                case ParserType.Soccer365:
                    league = await _soccer365parserService.GetLeagueDataByUrl(url);
                    break;
                case ParserType.Soccerway:
                    league = await _soccerwayParserService.GetLeagueDataByUrl(url);
                    break;
            }

            return league;
        }

        public async Task<List<Game>> GetActiveGamesByUrl(string url)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                List<Game> games = new List<Game>();

                var leagueParseData = await _leagueParseListService.GetByUrl(url);

                var league = _dataContext.Leagues.SingleOrDefault(l=>l.Url == url);

                if (leagueParseData != null)
                {
                    switch ((ParserType)leagueParseData.ParserType)
                    {
                        case ParserType.Soccer365:
                            games = await _soccer365parserService.GetActiveGamesByUrl(url, league.Title);
                            break;
                        case ParserType.Soccerway:
                            games = await _soccerwayParserService.GetActiveGamesByUrl(url, league.Title);
                            break;
                    }
                }
                else
                {
                    games = await _soccerwayParserService.GetActiveGamesByUrl(url, league.Title);
                }

                return games;
            }
        }

        public async Task<List<Game>> GetActiveGamesByUrl(WebDriver driver, string url, string leagueName, bool parseActions = true)
        {
            List<Game> games = new List<Game>();

            var leagueParseData = await _leagueParseListService.GetByUrl(url);

            if (leagueParseData != null)
            {
                switch ((ParserType)leagueParseData.ParserType)
                {
                    case ParserType.Soccer365:
                        games = await _soccer365parserService.GetActiveGamesByUrl(url, leagueName, parseActions);
                        break;
                    case ParserType.Soccerway:
                        games = await _soccerwayParserService.GetActiveGamesByUrl(driver, url, leagueName, parseActions);
                        break;
                }
            }
            else
            {
                games = await _soccerwayParserService.GetActiveGamesByUrl(driver, url, leagueName, parseActions);
            }

            return games;
        }

        public async Task<List<Game>> GetActiveGamesByUrl(string url, string leagueName, bool parseActions = true)
        {
            List<Game> games = new List<Game>();

            var leagueParseData = await _leagueParseListService.GetByUrl(url);

            if (leagueParseData != null)
            {
                switch ((ParserType)leagueParseData.ParserType)
                {
                    case ParserType.Soccer365:
                        games = await _soccer365parserService.GetActiveGamesByUrl(url, leagueName, parseActions);
                        break;
                    case ParserType.Soccerway:
                        games = await _soccerwayParserService.GetActiveGamesByUrl(url, leagueName, parseActions);
                        break;
                }
            }
            else
            {
                games = await _soccerwayParserService.GetActiveGamesByUrl(url, leagueName, parseActions);
            }

            return games;
        }

       


    }
}
