using cardscore_api.Data;
using cardscore_api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace cardscore_api.Services
{
    public class LeaguesService
    {
        private DataContext _context;
        private readonly LeagueParseListService _parserListService;
        private readonly Soccer365ParserService _soccer365parserService;
        private readonly ReglamentsService _reglamentsService;
        private readonly ParserService _parserService;
        private readonly RedisService _redisService;

        public LeaguesService(DataContext context, LeagueParseListService parseListService, Soccer365ParserService soccer365ParserService, ReglamentsService reglamentsService, ParserService parserService, RedisService redisService)
        {
            _context = context;
            _parserListService = parseListService;
            _soccer365parserService = soccer365ParserService;
            _reglamentsService = reglamentsService;
            _parserService = parserService;
            _redisService = redisService;
        }

        public async Task<League> Create(League league)
        {
            await _context.AddAsync(league);
            await _context.SaveChangesAsync();
            return league;
        }

        public async Task<League> Get(int id)
        {
            var league = await _context.Leagues.SingleOrDefaultAsync(r => r.Id == id);
            return league;
        }

        public async Task Edit(int id, League newData)
        {
            var league = await _context.Leagues.SingleOrDefaultAsync(r => r.Id == id);
            if (league != null)
            {
                league.Url = newData.Url;
                league.Title = newData.Title;
                league.Country = newData.Country;
                league.StartDate = newData.StartDate;
                league.EndDate = newData.EndDate;
                league.GamesCount = newData.GamesCount;
                league.Active = newData.Active;
            }

            _context.SaveChanges();
        }

        public async Task<League> GetByName(string name)
        {
            var league = await _context.Leagues.FirstOrDefaultAsync(r => r.Title == name);
            return league;
        }

        public async Task<League> GetByUrl(string url)
        {
            var league = await _context.Leagues.FirstOrDefaultAsync(r => r.Url == url);
            return league;
        }

        public async Task Remove(int id)
        {
            var league = await Get(id);
            var notificators = _context.UserNotificationOptions.Where(n => n.Name == league.Title).ToList() ;
            _context.UserNotificationOptions.RemoveRange(notificators);
            _context.Leagues.Remove(league);
            _context.SaveChanges();
        }

        public async Task<List<League>> GetManyByNames(List<string> names)
        {
            var data = new List<League>();

            foreach (var name in names)
            {
                var league = await GetByName(name);

                if (!data.Contains(league))
                {
                    data.Add(league);
                }
            }

            return data;
        }

        public async Task<List<League>> GetAll()
        {
            var leagues = await _context.Leagues.ToListAsync();
            return leagues;
        }

        public async Task ParseFromParseData()
        {
            var leagueParseDatas = await _parserListService.GetAll();
            var existLeagues = await _context.Leagues.ToListAsync();

            foreach (var league in leagueParseDatas)
            {
                if (existLeagues.Find(l => l.Url == league.Url) != null)
                {
                    continue;
                }

                League leagueData = await _parserService.GetLeagueDataByUrl(league.ParserType, league.Url);

                if (leagueData == null)
                {
                    continue;
                }

                var reglamentFromData = await _reglamentsService.GetByName(leagueData.Title);

                if (reglamentFromData == null)
                {
                    Reglament reglamentNew = new()
                    {
                        Name = leagueData.Title,
                        Text = "..."
                    };

                    await _context.AddAsync(reglamentNew);
                }

                await _context.AddAsync(leagueData);
                await _context.SaveChangesAsync();
            }
        }
    }
}
