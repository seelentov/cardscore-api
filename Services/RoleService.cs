using cardscore_api.Data;
using cardscore_api.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace cardscore_api.Services
{
    public class RoleService
    {
        private DataContext _context;
        public RoleService(DataContext context)
        {
            _context = context;
        }

        public async Task<List<Role>> GetAll()
        {
            var data = await _context.Roles.ToListAsync();
            return data;
        }

        public async Task<Role> GetByName(string name)
        {
            var role = await _context.Roles.SingleOrDefaultAsync(r => r.Name == name);
            return role;
        }

        public async Task Create(Role role)
        {
            await _context.AddAsync(role);
            await _context.SaveChangesAsync();
        }

    }
}
