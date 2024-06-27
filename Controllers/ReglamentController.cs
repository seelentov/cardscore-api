using cardscore_api.Models;
using cardscore_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace cardscore_api.Controllers
{
    [ApiController]
    [Route("api/reglament")]
    public class ReglamentController : ControllerBase
    {
        private readonly ReglamentsService _reglamentsService;
        private readonly ErrorsService _errorsService;
        private readonly RedisService _redisService;
        public ReglamentController(ReglamentsService reglamentsService, ErrorsService errorsService, RedisService redisService)
        {
            _reglamentsService = reglamentsService;
            _errorsService = errorsService;
            _redisService = redisService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var key = _redisService.CreateKeyFromRequest(Request);

                var data = await _redisService.Get<List<Reglament>>(key);

                if(data == null)
                {
                    data = await _reglamentsService.GetAll();
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

        [HttpGet("{name}")]
        public async Task<IActionResult> GetByName(string name)
        {
            try
            {
                var key = _redisService.CreateKeyFromRequest(Request);

                var data = await _redisService.Get<Reglament>(key);

                if(data == null)
                {
                    data = await _reglamentsService.GetByName(name);
                }

                if (data == null)
                {
                    return NotFound($"Регламент {name} не найден");
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
    }
}
