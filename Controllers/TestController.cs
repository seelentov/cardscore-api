using cardscore_api.Models;
using cardscore_api.Models.Dtos;
using cardscore_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace cardscore_api.Controllers
{
    [ApiController]
    [Route("api/test")]

    public class TestController: ControllerBase
    {
        private readonly LeaguesService _leaguesService;
        private readonly IHostEnvironment _environment;
        private readonly UserService _userService;
        private readonly ExpoNotificationsService _expoNotificationsService;
        private readonly NotificationsService _notificationsService;
        public TestController(LeaguesService leaguesService, IHostEnvironment environment, UserService userService, ExpoNotificationsService expoNotificationsService, NotificationsService notificationsService)
        {
            _leaguesService = leaguesService;
            _environment = environment;
            _userService = userService;
            _expoNotificationsService = expoNotificationsService;
            _notificationsService = notificationsService;
        }

        [HttpGet("parseLeagues")]
        public async Task<IActionResult> ParseLeagues()
        {
            if (!_environment.IsDevelopment())
            {
                return Unauthorized();
            }

            await _leaguesService.ParseFromParseData();
            return Ok();
        }

        [HttpGet("throwAllNotif")]
        public async Task<IActionResult> ThrowAllNotif()
        {
            if (!_environment.IsDevelopment())
            {
                return Unauthorized();
            }

            var users = await _userService.GetAll();

            foreach (var user in users)
            {
                await _expoNotificationsService.ThrowNotification("TEST", "test", user.ExpoToken);
            }

            await _leaguesService.ParseFromParseData();
            return Ok();
        }

    }
}
