using cardscore_api.Data;
using cardscore_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using System;
using System.Linq;
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
        private ChromeDriver _driver;
        private readonly SeleniumService _seleniumService;

        public GamesWorkerService(ErrorsService errorsService, IServiceScopeFactory scopeFactory, ILogger<GamesWorkerService> logger, SeleniumService seleniumService)
        {
            _errorsService = errorsService;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _seleniumService = seleniumService;
            _driver = seleniumService.GetDriver();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {

            _ = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    
                        using (var scope = _scopeFactory.CreateScope())
                        {
                        try
                        {
                            var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                            var _parserService = scope.ServiceProvider.GetRequiredService<ParserService>();
                            var _redisService = scope.ServiceProvider.GetRequiredService<RedisService>();

                            var leagues = _dataContext.Leagues.ToList();

                            leagues.Sort((x,y)=> y.Id.CompareTo(x.Id));

                            foreach (var league in leagues)
                            {
                                if(league.LastUpdate != null && league.LastUpdate > DateTime.UtcNow)
                                {
                                    continue;
                                }

                                var leagueTest = await _parserService.GetDataByUrl(_driver, league.Url, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

                                _logger.LogInformation(JsonSerializer.Serialize(leagueTest), Microsoft.Extensions.Logging.LogLevel.Information);
                                
                                _logger.LogInformation($"Saving {league.Title} \n", Microsoft.Extensions.Logging.LogLevel.Information);
                                var leagueData = await _parserService.GetDataByUrl(_driver, league.Url, DateTime.UtcNow.AddYears(-2));

                                if (leagueData != null)
                                {
                                    await _redisService.SetAsync("league:" + league.Url, JsonSerializer.Serialize(leagueData));
                                    _logger.LogInformation($"Save {league.Title} \n", Microsoft.Extensions.Logging.LogLevel.Information);
                                }

                                league.LastUpdate = DateTime.UtcNow.AddHours(72);
                                _dataContext.SaveChanges();

                               // await Task.Delay(TimeSpan.FromHours(3));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation("GamesWorkerError: " + ex.Message, Microsoft.Extensions.Logging.LogLevel.Error);
                            _logger.LogInformation("ReloadGamesWSession!", Microsoft.Extensions.Logging.LogLevel.Error);

                            await Task.Delay(TimeSpan.FromMinutes(30));

                            if (_driver != null)
                            {
                                _driver.Quit();
                            }

                            _driver = _seleniumService.GetDriver();

                        }
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
