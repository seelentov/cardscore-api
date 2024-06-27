using cardscore_api.Data;
using cardscore_api.Models;
using Microsoft.EntityFrameworkCore;

namespace cardscore_api.Services
{
    public class NotificationsService
    {
        private DataContext _context;
        public NotificationsService(DataContext context)
        {
            _context = context;
        }

        public async Task<List<Notification>> GetNotificationByUserId(int userId)
        {
            var data = await _context.Notifications.Where(n => n.UserId == userId).ToListAsync();
            return data;
        }

        public async Task Create(Notification notification)
        {
            _context.Notifications.Add(notification);
            _context.SaveChanges();
        }

        public async Task RemoveManyByGameUrlAndUserId(string gameUrl, int userId)
        {
            var data = await _context.Notifications.Where(n => (n.UserId == userId) && (n.GameUrl == gameUrl)).ToListAsync();
            _context.Notifications.RemoveRange(data);
            _context.SaveChanges();
        }

    }
}
