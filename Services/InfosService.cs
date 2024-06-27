using cardscore_api.Data;
using cardscore_api.Models;
using Microsoft.EntityFrameworkCore;

namespace cardscore_api.Services
{
    public class InfosService
    {
        private DataContext _context;
        public InfosService(DataContext context)
        {
            _context = context;
        }

        public async Task<Info> GetBySlug(string slug)
        {
            var info = await _context.Infos.SingleOrDefaultAsync(i => i.Slug == slug);
            return info;
        }
        
        public async Task<Info> GetContacts()
        {
            return await GetBySlug("contacts");
        }

        public async Task<Info> GetPayment()
        {
            return await GetBySlug("payment");
        }

        public async Task<Info> GetPolicy()
        {
            return await GetBySlug("policy");
        }

        public async Task EditBySlug(string slug, string desc)
        {
            var info = await _context.Infos.SingleOrDefaultAsync(i => i.Slug == slug);
            if(info == null)
            {
                throw new Exception($"Info {slug} not found");
            }

            info.Description = desc;
            await _context.SaveChangesAsync();
        }

        public async Task EditContacts(string desc)
        {
            await EditBySlug("contacts", desc);
        }

        public async Task EditPayment(string desc)
        {
            await EditBySlug("payment", desc);
        }

        public async Task EditPolicy(string desc)
        {
            await EditBySlug("policy", desc);
        }
    }
}
