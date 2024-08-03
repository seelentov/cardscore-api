using cardscore_api.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace cardscore_api.Services
{
    public class RedisService
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;

        public RedisService(IConfiguration configuration)
        {
            var redisConfiguration = configuration.GetSection("Redis").Get<RedisConfiguration>();
            _connectionMultiplexer = ConnectionMultiplexer.Connect(redisConfiguration.ConnectionString);
        }

        public async Task<string> GetAsync(string key)
        {
            var db = _connectionMultiplexer.GetDatabase();
            return await db.StringGetAsync(key);
        }

        public async Task SetAsync(string key, string value, TimeSpan? expiry = null)
        {
            var db = _connectionMultiplexer.GetDatabase();
            await db.StringSetAsync(key, value, expiry);
        }

        public async Task UpdateLeagueAsync(string url, string value)
        {
            var db = _connectionMultiplexer.GetDatabase();

            var league = await db.StringGetAsync("league:" + url);
            await db.StringSetAsync("league_active:" + url, value);

            var leagueSer = JsonSerializer.Deserialize<LeagueIncludeGames>(league);
            var activeGames = JsonSerializer.Deserialize<List<Game>>(value);

            var gamesDict = new Dictionary<string, Game>();

            foreach (var game in leagueSer.Games)
            {
                gamesDict[game.Url] = game;
            }

            foreach (var game in activeGames)
            {
                gamesDict[game.Url] = game;
            }

            var gamesList = gamesDict.Values.ToList();

            gamesList.Sort((a, b) => a.DateTime.CompareTo(b.DateTime));

            leagueSer.Games = gamesList;

            var newValue = JsonSerializer.Serialize(leagueSer);

            await db.StringSetAsync("league:" + url, newValue);
        }

        public async Task<LeagueIncludeGames>? GetCachedDataByUrl(string url, DateTime? startDate = null)
        {
            var startDateProxy = startDate;

            if (startDate == null)
            {
                startDateProxy = DateTime.UtcNow.AddDays(-4);
            }

            var cachedData = await GetAsync("league:" + url);

            if (cachedData == null)
            {
                return null!;
            }

            var cachedDataActive = await GetAsync("league_active:" + url);

            var gamesDict = new Dictionary<string, Game>();

            var cachedDataRes = new LeagueIncludeGames();

            var cachedDataSer = JsonSerializer.Deserialize<LeagueIncludeGames>(cachedData);

            cachedDataRes = cachedDataSer;

            foreach (var game in cachedDataSer.Games.Where(g => g.DateTime >= startDateProxy).ToList())
            {
                gamesDict[game.Url] = game;
            }

            if (cachedDataActive != null)
            {
                var cachedDataActiveSer = JsonSerializer.Deserialize<List<Game>>(cachedDataActive);

                foreach (var game in cachedDataActiveSer)
                {
                    gamesDict[game.Url] = game;
                }
            }
            var gamesList = gamesDict.Values.ToList();

            gamesList.Sort((a, b) => a.DateTime.CompareTo(b.DateTime));

            cachedDataRes.Games = gamesList;

            return cachedDataRes;
        }

    }

    public class RedisConfiguration
    {
        public string Host { get; set; }
        public int Port { get; set; }

        public string ConnectionString
        {
            get
            {
                return $"{Host}:{Port},abortConnect=false";
            }
        }
    }
}