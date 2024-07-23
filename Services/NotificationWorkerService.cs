using cardscore_api.Data;
using cardscore_api.Models;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using System.Net;
using System.Text.Json;

namespace cardscore_api.Services
{
    public class NotificationWorkerService : IHostedService
    {
        private readonly Soccer365ParserService _soccer365ParserService;
        private readonly AsyncService _asyncService;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ErrorsService _errorsService;
        private readonly ExpoNotificationsService _expoNotificationsService;
        private readonly SeleniumService _seleniumService;
        private readonly ILogger<NotificationWorkerService> _logger;
        private readonly int _timeFix = -2;
        private ChromeDriver _driver;

        public NotificationWorkerService(Soccer365ParserService soccer365ParserService, IServiceScopeFactory scopeFactory, ErrorsService errorsService, ExpoNotificationsService expoNotificationsService, ILogger<NotificationWorkerService> logger, SeleniumService seleniumService, AsyncService asyncService)
        {
            _soccer365ParserService = soccer365ParserService;
            _scopeFactory = scopeFactory;
            _errorsService = errorsService;
            _expoNotificationsService = expoNotificationsService;
            _logger = logger;

            _driver = seleniumService.GetDriver();

            _seleniumService = seleniumService;

            _asyncService = asyncService;
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
                            var _redisService = scope.ServiceProvider.GetRequiredService<RedisService>();

                            var oldCacheNotifications = _dataContext.CachedNotifications.Where(c => c.DateTime < DateTime.UtcNow.AddDays(-2)).ToList();

                            if (oldCacheNotifications != null && oldCacheNotifications.Count > 0)
                            {
                                _dataContext.CachedNotifications.RemoveRange(oldCacheNotifications);
                                _dataContext.SaveChanges();
                            }

                            var oldNotifications = _dataContext.Notifications.Where(c => c.DateTime < DateTime.UtcNow.AddDays(-10)).ToList();

                            if (oldNotifications != null && oldNotifications.Count > 0)
                            {
                                _dataContext.Notifications.RemoveRange(oldNotifications);
                                _dataContext.SaveChanges();
                            }

                            var leagues = await _dataContext.Leagues.ToListAsync();
                            foreach (var league in leagues)
                            {
                                var isTested = false;

                                if (league.NearestGame > DateTime.UtcNow && !isTested)
                                {
                                    continue;
                                }

                                _logger.LogInformation("CheckLeague: " + league.Title, Microsoft.Extensions.Logging.LogLevel.Information);


                                var activeGames = new List<Game>();
                                
                                activeGames = await GetActiveGames(league.Url, league.Title);

                                if (activeGames != null && activeGames.Count > 0)
                                {
                                    try
                                    {
                                        await _redisService.UpdateLeagueAsync(league.Url, JsonSerializer.Serialize(activeGames));
                                        _logger.LogInformation("SavedActive: " + league.Title, Microsoft.Extensions.Logging.LogLevel.Information);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogInformation("Error SavedActive: " + league.Title + " " + ex.Message, Microsoft.Extensions.Logging.LogLevel.Error);
                                    }
                                }

                                if (activeGames == null || activeGames.Count < 1)
                                {
                                    var leagueData = await _redisService.GetCachedDataByUrl(league.Url);

                                    if (leagueData != null)
                                    {
                                        var sortedGames = leagueData.Games.OrderBy(game => game.DateTime).ToList();
                                        var nearestGame = sortedGames.FirstOrDefault(game => game.DateTime >= DateTime.UtcNow);

                                        if (nearestGame != null)
                                        {
                                            league.NearestGame = nearestGame.DateTime;
                                        }
                                        else
                                        {
                                            league.NearestGame = DateTime.UtcNow.AddHours(12);
                                            _logger.LogInformation("Empty: " + league.Title, Microsoft.Extensions.Logging.LogLevel.Information);
                                        }
                                    }
                                    else
                                    {
                                        league.NearestGame = DateTime.UtcNow.AddHours(12);
                                        _logger.LogInformation("Empty: " + league.Title, Microsoft.Extensions.Logging.LogLevel.Information);
                                    };

                                    _logger.LogInformation("Wait: " + league.Title + " / " + league.NearestGame, Microsoft.Extensions.Logging.LogLevel.Information);
                                    _dataContext.SaveChanges();

                                    continue;
                                }

                                var options = _dataContext.UserNotificationOptions.Where(n => n.Name == league.Title).ToList();

                                foreach (var game in activeGames)
                                {
                                    if (game.Actions != null && game.Actions.Count > 0)
                                    {
                                        foreach (var action in game.Actions)
                                        {
                                            if (action.Player == null || action.Player.Name == null || action.Player.Name.ToLower().Contains("temporarily unavailable") || action.Player.Name == "нет данных")
                                            {
                                                continue;
                                            }

                                            foreach (var option in options)
                                            {
                                                var key = (game.Id + game.Teams[0].Name + game.Teams[1].Name + action.Player.Name + action.ActionType.ToString() + option.UserId).ToLower();

                                                _logger.LogInformation("Check Key: " + key, Microsoft.Extensions.Logging.LogLevel.Information);

                                                if (_dataContext.CachedNotifications.Where(c => c.Key == key).ToList().Count > 0)
                                                {
                                                    continue;
                                                }

                                                var isYellowCard = action.ActionType == GameActionType.YellowCard;

                                                var haveCardCount = action.Player.YellowCards != null;

                                                var isCriticalYellowCard = haveCardCount &&
                                                       ((action.Player.YellowCards) == option.CardCount) ||
                                                       ((action.Player.YellowCards) == option.CardCountTwo) ||
                                                       ((action.Player.YellowCards) == option.CardCountThree);

                                                int? criticalYellowCardCount = isCriticalYellowCard ?
                                                (((action.Player.YellowCards) == option.CardCount) ? 1 :
                                                ((action.Player.YellowCards) == option.CardCountTwo) ? 2 :
                                                        3) : null!;

                                                var isCreateNotif = isCriticalYellowCard || !isYellowCard;

                                                if (isCreateNotif)
                                                {
                                                    try
                                                    {
                                                        var player = await _redisService.GetAsync(action.Player.Url);

                                                        if(player == null)
                                                        {
                                                            await _redisService.SetAsync("player:" + action.Player.Url, JsonSerializer.Serialize(action.Player), TimeSpan.FromDays(2));
                                                            Console.WriteLine("SavedPlayer: " + action.Player.Name);
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        Console.WriteLine("Error SavedPlayer: " + action.Player.Name);
                                                    }

                                                    if (option.Active)
                                                    {
                                                        /*await ThrowNotification(game, action, option.UserId, league, criticalYellowCardCount);*/
                                                        _logger.LogInformation("\n\nThrowNotification: " + option.UserId + ", " + key + "\n\n", Microsoft.Extensions.Logging.LogLevel.Information);
                                                    }

                                                    await CreateNotificator(game, action, option.UserId);
                                                    _logger.LogInformation("\n\nCreateNotificator: " + option.UserId + ", " + key + "\n\n", Microsoft.Extensions.Logging.LogLevel.Information);
                                                }

                                                _dataContext.CachedNotifications.Add(new CachedNotification(key));
                                                _dataContext.SaveChanges();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if(_driver != null)
                        {
                            _driver.Quit();
                        }

                        _driver = _seleniumService.GetDriver();
                        _logger.LogInformation("NotifWorkerError: " + ex.Message, Microsoft.Extensions.Logging.LogLevel.Error);
                        _logger.LogInformation("ReloadNotifSession!", Microsoft.Extensions.Logging.LogLevel.Error);
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

        public async Task ThrowNotification(Game game, GameAction action, int userId, League league, int? criticalYellowCardCount = null)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                    var user = await _dataContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

                    if (user != null)
                    {
                        var title = $"{game.Teams[0].Name} - {game.Teams[1].Name} ({action.Player.Name})";

                        string criticalYellowCardCountLocalize = "";

                        switch (criticalYellowCardCount)
                        {
                            case 1:
                                criticalYellowCardCountLocalize = " первую"; break;
                            case 2:
                                criticalYellowCardCountLocalize = " вторую"; break;
                            case 3:
                                criticalYellowCardCountLocalize = " третью"; break;
                        }

                        string actionType = action.ActionType == GameActionType.YellowCard ? $"перебор желтых" :
                            action.ActionType == GameActionType.YellowRedCard ? $"получил вторую желтую карточку за матч (красную)" :
                          action.ActionType == GameActionType.Switch ? "покидает поле и его заменяет " + action.Player2.Name :
                          "получил красную карточку";

                        var body = $"{action.Player.Name} {actionType}. {action.Time} минута";

                        await _expoNotificationsService.ThrowNotification(title, body, user.ExpoToken);
                    }
                }
            }
            catch (Exception ex)
            {
                var title = $"{game.Teams[0].Name} - {game.Teams[1].Name} ({action.Player.Name})";

                _errorsService.CreateErrorFile(ex);
                _logger.LogInformation("\n\n\n\n\n\nThrowNotificationError: " + userId + ", " + title + "\n\n\n\n\n\n", Microsoft.Extensions.Logging.LogLevel.Error);
            }

        }

        public async Task CreateNotificator(Game game, GameAction action, int userId)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                    var gameKey = (game.Teams[0].Name + game.Teams[1].Name).Replace(" ", "");

                    if (action.ActionType == GameActionType.YellowRedCard || action.ActionType == GameActionType.RedCard)
                    {
                        var deletedNotif = _dataContext.Notifications.FirstOrDefault(n => n.GameUrl == gameKey && n.UserId == userId && n.PlayerName == action.Player.Name && n.LeftTeam == action.LeftTeam && n.ActionType == GameActionType.YellowCard);

                        var deletedNotif2 = _dataContext.Notifications.FirstOrDefault(n => n.GameUrl == gameKey && n.UserId == userId && n.PlayerName == action.Player.Name && n.LeftTeam == action.LeftTeam && n.ActionType == (action.ActionType == GameActionType.YellowRedCard ? GameActionType.RedCard : GameActionType.YellowRedCard));

                        if (deletedNotif != null)
                        {
                            _dataContext.Notifications.Remove(deletedNotif);
                        }

                        if (deletedNotif2 != null)
                        {
                            _dataContext.Notifications.Remove(deletedNotif2);
                        }
                    }

                    Notification notif = new()
                    {
                        GameUrl = gameKey,
                        UserId = userId,
                        LeftTeam = action.LeftTeam,
                        Name = DateTime.UtcNow + " " + action.Player.Name + " " + action.ActionType,
                        ActionType = action.ActionType,
                        PlayerUrl = action.Player.Url != null ? action.Player.Url : "",
                        PlayerName = action.Player.Name,
                        Time = action.Time,
                        PlayerUrl2 = action.Player2 != null ? action.Player2?.Url : "",
                        PlayerName2 = action.Player2 != null ? action.Player2?.Name : "",
                        DateTime = DateTime.UtcNow
                    };

                    _dataContext.Notifications.Add(notif);
                    _dataContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _errorsService.CreateErrorFile(ex);
            }

        }

        public async Task CreateTxt(Game game, GameAction action, int userId)
        {
            try
            {
                string directory = Path.Combine(Directory.GetCurrentDirectory(), "Notificators");

                string fileName = string.Concat($"{game.Teams[0].Name}-{game.Teams[1].Name}_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{userId}_{action.ActionType}.txt".Split(Path.GetInvalidFileNameChars()));

                string filePath = Path.Combine(directory, fileName);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }


                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine(JsonSerializer.Serialize(action) + "\n" + JsonSerializer.Serialize(game) + '\n' + "USERID:" + userId);
                }
            }
            catch (Exception ex)
            {
                _errorsService.CreateErrorFile(ex);
            }
        }


        public async Task<List<Game>> GetDataByNotificator(UserNotificationOption notificator)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                var league = await _dataContext.Leagues.FirstOrDefaultAsync(l => l.Title == notificator.Name);

                List<Game> games = new();

                if (league != null)
                {
                    games = await GetActiveGames(league.Url, league.Title);
                }

                return games;
            }
        }
        public async Task<League> GetLeagueByNotificator(UserNotificationOption notificator)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                var league = await _dataContext.Leagues.FirstOrDefaultAsync(l => l.Title == notificator.Name);

                return league;
            }
        }

        public async Task<List<Game>> GetActiveGames(string url, string name)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                var _parserService = scope.ServiceProvider.GetRequiredService<ParserService>();

                var parseData = await _dataContext.LeagueParseDatas.FirstOrDefaultAsync(l => l.Url == url);

                List<Game> activeGames = await _parserService.GetActiveGamesByUrl(_driver, parseData.Url, name);

                return activeGames;
            }
        }
    }
}
