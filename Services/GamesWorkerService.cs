using cardscore_api.Data;
using cardscore_api.Models;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using System;
using System.Text.Json;
using static System.Collections.Specialized.BitVector32;

namespace cardscore_api.Services
{
    public class GamesWorkerService : IHostedService
    {

        private readonly ILogger<GamesWorkerService> _logger;
        private readonly int _timeFix = -2;

        private readonly ErrorsService _errorsService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly FirefoxDriver _driver;
        private readonly SeleniumService _seleniumService;

        public GamesWorkerService(ErrorsService errorsService, IServiceScopeFactory scopeFactory, ILogger<GamesWorkerService> logger, SeleniumService seleniumService)
        {
            _errorsService = errorsService;
            _scopeFactory = scopeFactory;
            _logger = logger;

            _driver = seleniumService.GetDriver();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {

            _ = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                            var _parserService = scope.ServiceProvider.GetRequiredService<ParserService>();
                            var _redisService = scope.ServiceProvider.GetRequiredService<RedisService>();

                            var leagues = _dataContext.Leagues.ToList();

                            leagues.Sort((x,y)=> y.Id.CompareTo(x.Id));

                            foreach (var league in leagues)
                            {
                                _logger.LogInformation($"Saving {league.Title} \n", Microsoft.Extensions.Logging.LogLevel.Information);
                                var leagueData = await _parserService.GetDataByUrl(_driver, league.Url, DateTime.UtcNow.AddYears(-2));

                                if(leagueData != null)
                                {
                                    await _redisService.SetAsync("league:" + league.Url, JsonSerializer.Serialize(leagueData), TimeSpan.FromDays(1));
                                    _logger.LogInformation($"Save {league.Title} \n", Microsoft.Extensions.Logging.LogLevel.Information);
                                }
                            }

                            await Task.Delay(TimeSpan.FromDays(12));
                        }
                    }
                    catch (Exception ex)
                    {
                        _errorsService.CreateErrorFile(ex);
                    }
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _driver.Close();
            _driver.Quit();

            return Task.CompletedTask;
        }
    }
}
