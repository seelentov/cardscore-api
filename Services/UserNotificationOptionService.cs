using cardscore_api.Data;
using cardscore_api.Models;
using cardscore_api.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace cardscore_api.Services
{
    public class UserNotificationOptionService
    {
        private DataContext _context;
        private readonly UserService _userService;

        public UserNotificationOptionService(DataContext context, UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public async Task<List<UserNotificationOptionResponse>> GetOptionsByUserId(int userId)
        {
            var option = await _context.UserNotificationOptions.Where(o => o.UserId == userId)
                .Select(o => new UserNotificationOptionResponse()
                {
                    Name = o.Name,
                    CardCount = o.CardCount,
                    CardCountTwo = o.CardCountTwo,
                    CardCountThree = o.CardCountThree,
                    Active = o.Active
                })
                .ToListAsync();
            return option;
        }

        public async Task<List<UserNotificationOption>> GetAllOptions()
        {
            var options = await _context.UserNotificationOptions.ToListAsync();
            return options;
        }

        public async Task UpdateOptionsByUserId(int userId, EditUserNotificationOptionsDto options)
        {
            foreach (var option in options.Options)
            {
                var optionData = await _context.UserNotificationOptions.FirstOrDefaultAsync(o => o.User.Id == userId && o.Name == option.Name);

                if (optionData != null)
                {
                    optionData.CardCount = option.CardCount;
                    optionData.CardCountTwo = option.CardCountTwo;
                    optionData.CardCountThree = option.CardCountThree;
                    optionData.Active = option.Active;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<UserNotificationOption> GetOptionByNameAndUserId(int userId, string name)
        {
            var option = await _context.UserNotificationOptions.FirstOrDefaultAsync(o => o.UserId == userId && o.Name == name);
            return option;
        }

        public async Task Create(int userId, string name)
        {
            var user = await _userService.GetById(userId);

            UserNotificationOption userNotificationOption = new()
            {
                User = user,
                Name = name,
                CardCount = 3,
                CardCountTwo = 6,
                CardCountThree = 9,
                UserNotificationOptionType = UserNotificationOptionType.YellowCard,
            };

            await _context.AddAsync(userNotificationOption);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int userId, string name)
        {
            var option = await GetOptionByNameAndUserId(userId, name);

            _context.UserNotificationOptions.Remove(option);
            await _context.SaveChangesAsync();
        }
    }
}
