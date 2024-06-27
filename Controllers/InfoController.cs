using cardscore_api.Models;
using cardscore_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace cardscore_api.Controllers
{
    [ApiController]
    [Route("api/info")]
    public class InfoController : ControllerBase
    {
        private readonly InfosService _infoService;
        private readonly ILogger<InfoController> _logger;
        private readonly ErrorsService _errorsService;
        private readonly RedisService _redisService;
        public InfoController(InfosService infoService, ILogger<InfoController> logger, ErrorsService errorsService, RedisService redisService)
        {
            _infoService = infoService;
            _logger = logger;
            _errorsService = errorsService;
            _redisService = redisService;
        }

        [HttpGet("contacts")]
        public async Task<IActionResult> GetContacts()
        {
            try
            {
                var key = _redisService.CreateKeyFromRequest(Request);

                var data = await _redisService.Get<Info>(key);

                if(data == null)
                {
                    data = await _infoService.GetContacts();
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

        [HttpGet("payment")]
        public async Task<IActionResult> GetPayment()
        {
            try
            {
                var key = _redisService.CreateKeyFromRequest(Request);

                var data = await _redisService.Get<Info>(key);

                if (data == null)
                {
                    data = await _infoService.GetPayment();
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

        [HttpGet("policy")]
        public async Task<IActionResult> GetPolicy()
        {
            try
            {
                var key = _redisService.CreateKeyFromRequest(Request);

                var data = await _redisService.Get<Info>(key);

                if (data == null)
                {
                    data = await _infoService.GetPolicy();
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
    }
}
