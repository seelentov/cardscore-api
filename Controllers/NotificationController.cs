using cardscore_api.Models.Dtos;
using cardscore_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace cardscore_api.Controllers
{
    [ApiController]
    [Route("api/notification")]
    public class NotificationController: ControllerBase
    {
        private readonly ILogger<NotificationController> _logger;
        private readonly UserNotificationOptionService _userNotificationOptionService;
        private readonly UserService _userService;
        private readonly ErrorsService _errorsService;

        public NotificationController(ILogger<NotificationController> logger, UserNotificationOptionService userNotificationOptionService, UserService userService, ErrorsService errorsService)
        {
            _logger = logger;
            _userNotificationOptionService = userNotificationOptionService;
            _userService = userService;
            _errorsService = errorsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            try
            {
                string userId = Request.Headers["UserId"].FirstOrDefault()?.Split(" ").Last();

                if (userId == null)
                {
                    return Unauthorized(new { Message = "Заголовок Authorization пуст", userId });
                }

                var user = await _userService.GetById(int.Parse(userId));
                if (user == null)
                {
                    return Unauthorized(new { Message = "Пользователь не найден", userId });
                }

                var options = await _userNotificationOptionService.GetOptionsByUserId(user.Id);
                return Ok(options);
            }
            catch (Exception e)
            {
                _errorsService.CreateErrorFile(e);

                return StatusCode(500, e);
            }
        }


        [HttpPatch]
        public async Task<IActionResult> ChangeSettings(EditUserNotificationOptionsDto editUserNotificationOptionsDto)
        {
            try
            {
                string userId = Request.Headers["UserId"].FirstOrDefault()?.Split(" ").Last();

                if (userId == null)
                {
                    return Unauthorized(new { Message = "Заголовок Authorization пуст", userId });
                }

                var user = await _userService.GetById(int.Parse(userId));

                if (user == null)
                {
                    return Unauthorized(new { Message = "Пользователь не найден", userId });
                }

                await _userNotificationOptionService.UpdateOptionsByUserId(user.Id, editUserNotificationOptionsDto);
                var options = await _userNotificationOptionService.GetOptionsByUserId(user.Id);
                return Ok(options);
            }
            catch (Exception e)
            {
                _errorsService.CreateErrorFile(e);

                return StatusCode(500, e);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadSetting(string name)
        {
            try
            {
                string userId = Request.Headers["UserId"].FirstOrDefault()?.Split(" ").Last();

                if (userId == null)
                {
                    return Unauthorized(new { Message = "Заголовок Authorization пуст", userId });
                }

                var user = await _userService.GetById(int.Parse(userId));

                if (user == null)
                {
                    return Unauthorized(new { Message = "Пользователь не найден", userId });
                }

                await _userNotificationOptionService.Create(user.Id, name);
                return Ok();
            }
            catch (Exception e)
            {
                _errorsService.CreateErrorFile(e);

                return StatusCode(500, e);
            }
        }


        [HttpDelete]
        public async Task<IActionResult> DeleteSetting(string name)
        {
            try
            {
                string userId = Request.Headers["UserId"].FirstOrDefault()?.Split(" ").Last();

                if (userId == null)
                {
                    return Unauthorized(new { Message = "Заголовок Authorization пуст", userId });
                }

                var user = await _userService.GetById(int.Parse(userId));

                if (user == null)
                {
                    return Unauthorized(new { Message = "Пользователь не найден", userId });
                }

                await _userNotificationOptionService.Delete(user.Id, name);
                return Ok();
            }
            catch (Exception e)
            {
                _errorsService.CreateErrorFile(e);

                return StatusCode(500, e);
            }
        }   
    }
}
