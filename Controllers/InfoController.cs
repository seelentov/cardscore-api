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
        public InfoController(InfosService infoService, ILogger<InfoController> logger, ErrorsService errorsService)
        {
            _infoService = infoService;
            _logger = logger;
            _errorsService = errorsService;
        }

        [HttpGet("contacts")]
        public async Task<IActionResult> GetContacts()
        {
            try
            {
                var data = await _infoService.GetContacts();
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
                var data = await _infoService.GetPayment();
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
                var data = await _infoService.GetPolicy();
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
