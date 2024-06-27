using cardscore_api.Data;
using cardscore_api.Models;
using cardscore_api.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace cardscore_api.Services
{
    public class ReglamentsService
    {
        private DataContext _context;   
        public ReglamentsService(DataContext context)
        {
            _context = context;
        }

        public async Task<Reglament> Get(int id)
        {
            var data = await _context.Reglaments.SingleOrDefaultAsync(r=> r.Id == id);
            return data;
        }
        public async Task<Reglament> GetByName(string name)
        {
            var reglament = await _context.Reglaments.FirstOrDefaultAsync(x => x.Name == name);
            return reglament;
        }



        public async Task<List<Reglament>> GetAll()
        {
            var reglament = await _context.Reglaments.ToListAsync();
            return reglament;
        }

        public async Task Remove(int id)
        {
            var reg = await Get(id);
            _context.Reglaments.Remove(reg);
            _context.SaveChanges();
        }

        public async Task<Reglament> EditByName(string name, EditReglamentDto newReglament)
        {
            var reglament = await _context.Reglaments.FirstOrDefaultAsync(x => x.Name == name);
            reglament.Text = newReglament.Text;
            _context.SaveChanges();
            return reglament;
        }
    }
}
