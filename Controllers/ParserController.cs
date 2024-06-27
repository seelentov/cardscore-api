using cardscore_api.Attributes;
using cardscore_api.Models;
using cardscore_api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;

namespace cardscore_api.Controllers
{
    [ApiController]
    [Route("api/parser")]
    public class ParserController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly Soccer365ParserService _soccer365parserService;
        private readonly LeagueParseListService _parserListService;
        private readonly UrlService _urlService;
        private readonly LeaguesService _leaguesService;
        private readonly ErrorsService _errorsService;
        private readonly RedisService _redisService;
        private readonly DateTime _startDate = DateTime.UtcNow.AddDays(-4);
        public ParserController(ILogger<AuthController> logger, Soccer365ParserService soccer365parserService, LeagueParseListService parserListService, LeaguesService leaguesService, UrlService urlService, ErrorsService errorsService, RedisService redisService)
        {
            _logger = logger;
            _soccer365parserService = soccer365parserService;
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
                var key = _redisService.CreateKeyFromRequest(Request);

                var data = await _redisService.Get<League>(key);

                if (data == null)
                {
                    data = await _leaguesService.Get(id);
                }

                if (data == null)
                {
                    return NotFound();
                }

                await _redisService.SetOneMins(key, data);
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
                var key = _redisService.CreateKeyFromRequest(Request);

                var data = await _redisService.Get<List<League>>(key);

                if (data == null)
                {
                    data = await _leaguesService.GetAll();
                    await _redisService.SetOneMins(key, data);
                }
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
                var key = _redisService.CreateKeyFromRequest(Request);

                var data = await _redisService.Get<LeagueIncludeGames>(key);

                if (data == null)
                {
                    var normalizeUrl = _urlService.NormalizeUrl(url);

                    data = null!;

                    var leagueParseData = await _parserListService.GetByUrl(normalizeUrl);

                    if (leagueParseData == null)
                    {
                        return BadRequest(new { noParser = $"Лига по url {normalizeUrl} не найдена" });
                    }


                    switch (leagueParseData.ParserType)
                    {
                        case (int)ParserType.Soccer365:
                            data = await _soccer365parserService.GetDataByUrl(leagueParseData.Url, _startDate);
                            break;
                    }

                    if (data == null)
                    {
                        return BadRequest(new { noParser = $"Для лиги {leagueParseData.Name} отсутствует парсер" });
                    }
                }
                await _redisService.SetOneMins(key, data);
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
                Game gameData = null!;

                var normalizeUrlLeague = _urlService.NormalizeUrl(leagueUrl);
                var normalizeUrlGame = _urlService.NormalizeUrl(gameUrl);

                League leagueData;

                var leagueParseData = await _parserListService.GetByUrl(normalizeUrlLeague);

                if (leagueParseData == null)
                {
                    return BadRequest(new { noParser = $"Лига по url {normalizeUrlLeague} не найдена" });
                }

                switch (leagueParseData.ParserType)
                {
                    case (int)ParserType.Soccer365:
                        leagueData = await _leaguesService.GetByUrl(leagueParseData.Url);
                        gameData = await _soccer365parserService.ParseGameByPage(normalizeUrlGame, leagueData.Title, false);
                        break;
                }


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
                Player player = null!;

                var normalizeUrlLeague = _urlService.NormalizeUrl(leagueUrl);
                var normalizeUrlPlayer = _urlService.NormalizeUrl(playerUrl);

                var leagueParseData = await _parserListService.GetByUrl(normalizeUrlLeague);

                if (leagueParseData == null)
                {
                    return BadRequest(new { noParser = $"Лига по url {normalizeUrlLeague} не найдена" });
                }

                switch (leagueParseData.ParserType)
                {
                    case (int)ParserType.Soccer365:
                        var leagueData = await _leaguesService.GetByUrl(leagueParseData.Url);
                        player = await _soccer365parserService.ParsePlayerByPage(normalizeUrlPlayer, leagueData.Title);
                        break;
                }


                if (player == null)
                {
                    return BadRequest(new { noParser = $"Не удалось получить игрока {playerUrl} из {leagueUrl}" });
                }

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
