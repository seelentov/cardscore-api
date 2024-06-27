using System.Text.Json;
using Telegram.Bot.Requests.Abstractions;

namespace cardscore_api.Services
{
    public class ErrorsService
    {
        private readonly string _errorsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Errors");
        private readonly ILogger _logger;

        public async void CreateErrorFile(object errorData)
        {
            string fileName = string.Concat($"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt".Split(Path.GetInvalidFileNameChars()));
            string filePath = Path.Combine(_errorsDirectory, fileName);

            if (!Directory.Exists(_errorsDirectory))
            {
                Directory.CreateDirectory(_errorsDirectory);
            }

            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine(errorData.ToString());
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при записи в файл: {ex.Message}");
            }

        }
    }
}
