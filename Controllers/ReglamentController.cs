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
        public ReglamentController(ReglamentsService reglamentsService, ErrorsService errorsService)
        {
            _reglamentsService = reglamentsService;
            _errorsService = errorsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var data = await _reglamentsService.GetAll();

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
                var data = await _reglamentsService.GetByName(name);
                
                if (data == null)
                {
                    return NotFound($"Регламент {name} не найден");
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
