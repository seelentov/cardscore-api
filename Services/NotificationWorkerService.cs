using cardscore_api.Data;
using cardscore_api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;
using static System.Collections.Specialized.BitVector32;

namespace cardscore_api.Services
{
    public class NotificationWorkerService : IHostedService
    {
        private readonly Soccer365ParserService _soccer365ParserService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ErrorsService _errorsService;
        private readonly ExpoNotificationsService _expoNotificationsService;
        private readonly int _timeFix = -2;
        private readonly int _delay = 180000;
        public NotificationWorkerService(Soccer365ParserService soccer365ParserService, IServiceScopeFactory scopeFactory, ErrorsService errorsService, ExpoNotificationsService expoNotificationsService)
        {
            _soccer365ParserService = soccer365ParserService;
            _scopeFactory = scopeFactory;
            _errorsService = errorsService;
            _expoNotificationsService = expoNotificationsService;
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

                            var oldCacheNotifications = _dataContext.CachedNotifications.Where(c => c.DateTime < DateTime.UtcNow.AddDays(-2)).ToList();

                            if (oldCacheNotifications != null && oldCacheNotifications.Count > 0)
                            {
                                _dataContext.CachedNotifications.RemoveRange(oldCacheNotifications);
                                _dataContext.SaveChanges();
                            }

                            var notificators = await _dataContext.UserNotificationOptions.ToListAsync();

                            foreach (var option in notificators)
                            {
                                var activeGames = await GetDataByNotificator(option);

                                if (activeGames == null || activeGames.Count < 1)
                                {
                                    continue;
                                }

                                var league = await GetLeagueByNotificator(option);

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

                                            var key = game.Teams[0].Name + game.Teams[1].Name + action.Time + action.Player.Name + action.ActionType.ToString() + option.UserId;

                                            if (_dataContext.CachedNotifications.Where(c => c.Key == key).ToList().Count > 0)
                                            {
                                                continue;
                                            }
                                            else
                                            {
                                                _dataContext.CachedNotifications.Add(new CachedNotification(key));
                                                _dataContext.SaveChanges();
                                            }

                                            var isYellowCard = action.ActionType == GameActionType.YellowCard;

                                            var haveCardCount = action.Player.YellowCards != null;

                                            var isCriticalYellowCard = haveCardCount &&
                                                   ((action.Player.YellowCards + (action.Player.YellowRedCards * 2)) == option.CardCount) ||
                                                   ((action.Player.YellowCards + (action.Player.YellowRedCards * 2)) == option.CardCountTwo) ||
                                                   ((action.Player.YellowCards + (action.Player.YellowRedCards * 2)) == option.CardCountThree);

                                            int? criticalYellowCardCount = isCriticalYellowCard ?
                                            (((action.Player.YellowCards + (action.Player.YellowRedCards * 2)) == option.CardCount) ? 1 :
                                            ((action.Player.YellowCards + (action.Player.YellowRedCards * 2)) == option.CardCountTwo) ? 2 :
                                                    3) : null!;

                                            var isCreateNotif = isCriticalYellowCard || !isYellowCard;

                                            if (isCreateNotif)
                                            {
                                                if (option.Active)
                                                {
                                                    await ThrowNotification(game, action, option.UserId, league, criticalYellowCardCount);
                                                }

                                                await CreateNotificator(game, action, option.UserId);
                                            }
                                        }
                                    }
                                }
                                await Task.Delay(60000);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _errorsService.CreateErrorFile(ex);
                        Console.WriteLine(ex.Message);
                    }
                }
            }, cancellationToken);


            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
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
                            action.ActionType == GameActionType.YellowRedCard && criticalYellowCardCount != null ? $"получил вторую желтую карточку за матч (красную), перебор желтых" :
                          action.ActionType == GameActionType.YellowRedCard ? "получил вторую желтую карточку за матч (красную)" :
                          action.ActionType == GameActionType.Switch ? "покидает поле и его заменяет " + action.Player2.Name :
                          "получил красную карточку";

                        var body = $"{action.Player.Name} {actionType}. {action.Time} минута";

                        await _expoNotificationsService.ThrowNotification(title, body, user.ExpoToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _errorsService.CreateErrorFile(ex);
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

                    Notification notif = new()
                    {
                        GameUrl = gameKey,
                        UserId = userId,
                        LeftTeam = action.LeftTeam,
                        Name = DateTime.UtcNow.ToString() + " " + action.Player.Name + " " + action.ActionType,
                        ActionType = action.ActionType,
                        PlayerUrl = action.Player.Url != null ? action.Player.Url : "",
                        PlayerName = action.Player.Name,
                        Time = action.Time,
                        PlayerUrl2 = action.Player2 != null ? action.Player2?.Url : "",
                        PlayerName2 = action.Player2 != null ? action.Player2?.Name : "",
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

                var games = await GetActiveGames(league.Url, league.Title);

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
                var parseData = await _dataContext.LeagueParseDatas.FirstOrDefaultAsync(l => l.Url == url);
                List<Game> activeGames = new();

                switch (parseData.ParserType)
                {
                    case (int)ParserType.Soccer365:
                        activeGames = await _soccer365ParserService.GetActiveGamesByUrl(parseData.Url, name);
                        break;
                }

                return activeGames;
            }
        }
    }
}
