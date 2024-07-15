using cardscore_api.Data;
using cardscore_api.Models;
using Telegram.Bot.Types;

namespace cardscore_api.Services
{
    public class BaseOptionsService
    {
        private DataContext _context;
        private string _testDaysString = "TestDays";
        public BaseOptionsService(DataContext context)
        {
            _context = context;
        }

        private void UpdateOption(string key, int value)
        {
            var option = _context.BaseOptions.FirstOrDefault(o => o.Key == key);
            option.Value = value;
            _context.SaveChanges();
        }
        private BaseOption GetOption(string key)
        {
            var option = _context.BaseOptions.FirstOrDefault(o => o.Key == key);
            return option;
        }
        public BaseOption GetTestDays()
        {
            return GetOption(_testDaysString);
        }
        public void UpdateTestDays(int value)
        {
            UpdateOption(_testDaysString, value);
        }
    }
}
