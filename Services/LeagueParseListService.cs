using cardscore_api.Data;
using cardscore_api.Models;
using Microsoft.EntityFrameworkCore;

namespace cardscore_api.Services
{
    public class LeagueParseListService
    {
        private DataContext _context;

        public LeagueParseListService(DataContext context)
        {
            _context = context;
        }

        public async Task<List<LeagueParseData>> GetAll()
        {
            return _context.LeagueParseDatas.ToList();
        }

        public async Task<LeagueParseData> Get(int id)
        {
            var data = await _context.LeagueParseDatas.SingleOrDefaultAsync(x => x.Id == id);
            return data;
        }

        public async Task Edit(int id, LeagueParseData newData)
        {
            var parser = await _context.LeagueParseDatas.SingleOrDefaultAsync(x => x.Id == id);
            if(parser != null)
            {
                parser.Name = newData.Name;
                parser.Url = newData.Url;
                parser.ParserType = newData.ParserType;
            }

            _context.SaveChanges();
        }


        public async Task Remove(int id)
        {
            var data = await Get(id);
            _context.LeagueParseDatas.Remove(data);
            _context.SaveChanges();
        }

        public async Task<LeagueParseData> GetByUrl(string url)
        {
            var leagueParseData = await _context.LeagueParseDatas.SingleOrDefaultAsync(r => r.Url == url);
            return leagueParseData;
        }

        public async Task Create(LeagueParseData leagueParseData)
        {
            await _context.AddAsync(leagueParseData);
            await _context.SaveChangesAsync();
        }


    }
}
