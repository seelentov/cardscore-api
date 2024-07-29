using cardscore_api.Models;
using cardscore_api.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json;

namespace cardscore_api.Controllers
{
    [ApiController]
    [Route("api/parser")]
    public class ParserController : ControllerBase
    {
        private readonly ILogger<ParserController> _logger;
        private readonly ParserService _parserService;
        private readonly LeagueParseListService _parserListService;
        private readonly UrlService _urlService;
        private readonly LeaguesService _leaguesService;
        private readonly ErrorsService _errorsService;
        private readonly RedisService _redisService;
        public ParserController(ILogger<ParserController> logger, ParserService parserService, LeagueParseListService parserListService, LeaguesService leaguesService, UrlService urlService, ErrorsService errorsService, RedisService redisService)
        {
            _logger = logger;
            _parserService = parserService;
            _parserListService = parserListService;
            _leaguesService = leaguesService;
            _urlService = urlService;
            _errorsService = errorsService;
            _redisService = redisService;
        }

        [HttpGet("leagues/{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var data = await _leaguesService.Get(id);

                if (data == null)
                {
                    return NotFound();
                }

                return Ok(data);
            }
            catch (Exception e)
            {
                _errorsService.CreateErrorFile(e);

                return StatusCode(500, e);
            }
        }

        [HttpGet("leagues")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var data = await _leaguesService.GetAll();

                return Ok(data);
            }
            catch (Exception e)
            {
                _errorsService.CreateErrorFile(e);

                return StatusCode(500, e);
            }
        }


        [HttpGet("games/{url}")]
        public async Task<IActionResult> GetGames(string url)
        {
            try
            {
                var normalizeUrl = _urlService.NormalizeUrl(url);

                var cachedData = await _redisService.GetCachedDataByUrl(normalizeUrl, DateTime.UtcNow.AddDays(-4));
                

                if (cachedData != null)
                {
                    cachedData.GamesCount = cachedData.Games.Count;
                    return Ok(cachedData);
                }

                LeagueIncludeGames data = await _parserService.GetDataByUrl(normalizeUrl);

                if (data == null)
                {
                    return BadRequest(new { noParser = $"Для лиги {normalizeUrl} отсутствует парсер" });
                }

                return Ok(data);
            }
            catch (Exception e)
            {
                _errorsService.CreateErrorFile(e);

                return StatusCode(500, e);
            }
        }


        [HttpGet("game/{leagueUrl}/{gameUrl}")]
        public async Task<IActionResult> GetGame(string gameUrl, string leagueUrl)
        {
            try
            {
                var normalizeUrlLeague = _urlService.NormalizeUrl(leagueUrl);
                var normalizeUrlGame = _urlService.NormalizeUrl(gameUrl);

                var cachedLeague = await _redisService.GetCachedDataByUrl(normalizeUrlLeague);

                if(cachedLeague != null)
                {
                    var thisGame = cachedLeague.Games.FirstOrDefault(g=>g.Url == normalizeUrlGame);

                    return Ok(thisGame);
                }
                
                League leagueData = await _leaguesService.GetByUrl(normalizeUrlLeague);

                Game gameData = await _parserService.ParseGameByPage(normalizeUrlGame, leagueData.Title);

                if (gameData == null)
                {
                    return BadRequest(new { noParser = $"Для лиги {gameUrl} отсутствует парсер" });
                }

                return Ok(gameData);
            }
            catch (Exception e)
            {
                _errorsService.CreateErrorFile(e);

                return StatusCode(500, e);
            }
        }

        [HttpGet("player/{leagueUrl}/{playerUrl}")]
        public async Task<IActionResult> GetPlayer(string leagueUrl, string playerUrl)
        {
            try
            {
                var normalizeUrlLeague = _urlService.NormalizeUrl(leagueUrl);
                var normalizeUrlPlayer = _urlService.NormalizeUrl(playerUrl);

                var cachedPlayer = await _redisService.GetAsync("player:" + normalizeUrlPlayer);

                if (cachedPlayer != null)
                {
                    var playerCached = JsonSerializer.Deserialize<Player>(cachedPlayer);
                    return Ok(playerCached);
                }

                League leagueData = await _leaguesService.GetByUrl(normalizeUrlLeague);

                Player player = await _parserService.ParsePlayerByPage(normalizeUrlPlayer, leagueData.Title);

                if (player == null)
                {
                    return BadRequest(new { noParser = $"Не удалось получить игрока {playerUrl} из {leagueUrl}" });
                }

                await _redisService.SetAsync("player:" + normalizeUrlPlayer, JsonSerializer.Serialize(player), TimeSpan.FromDays(2));
                return Ok(player);
            }
            catch (Exception e)
            {
                _errorsService.CreateErrorFile(e);

                return StatusCode(500, e);
            }
        }
    }
}
