namespace cardscore_api.Services
{
    public class ErrorsService
    {
        private readonly string _errorsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Errors");
        private readonly ILogger<ErrorsService> _logger;

        public ErrorsService(ILogger<ErrorsService> logger)
        {
            _logger = logger;
        }

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
                var backupFiles = Directory.EnumerateFiles(_errorsDirectory, "*.txt");

                if (backupFiles.Count() >= 30)
                {
                    var latestBackupFile = backupFiles.OrderByDescending(f => f).Last();
                    File.Delete(latestBackupFile);
                }

                string formattedErrorData = $"Error Type: {errorData.GetType().Name}, Message: {errorData.ToString()}, StackTrace: {((Exception)errorData).StackTrace}";

                _logger.LogInformation(formattedErrorData, LogLevel.Critical);

                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine(formattedErrorData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Ошибка при записи в файл: {ex.Message}", LogLevel.Critical);
            }

        }

    }
}
