using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace cardscore_api.Services
{
    public class BackupService : IHostedService
    {
        private readonly string _backupPath;
        private static int count = 0;
        public BackupService()
        {
            _backupPath = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await CreateBackupAsync();
                    await Task.Delay(TimeSpan.FromDays(3), cancellationToken);
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task CreateBackupAsync()
        {
            try
            {
                if (!Directory.Exists(_backupPath))
                {
                    Directory.CreateDirectory(_backupPath);
                }

                var backupFileName = string.Concat($"{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_" + count++}_db.db".Split(Path.GetInvalidFileNameChars()));
                var backupFilePath = Path.Combine(_backupPath, backupFileName);

                File.Copy("db.db", backupFilePath, true);

                var backupFiles = Directory.EnumerateFiles(_backupPath, "*.db");

                if (backupFiles.Count() >= 2)
                {
                    var latestBackupFile = backupFiles.OrderByDescending(f => f).Last();
                    File.Delete(latestBackupFile);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании резервной копии: {ex.Message}");
            }
        }
    }
}
