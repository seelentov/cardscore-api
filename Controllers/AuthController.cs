using cardscore_api.Models;
using cardscore_api.Models.Dtos;
using cardscore_api.Models.LeagueParser;
using cardscore_api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace cardscore_api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {

        private readonly ILogger<AuthController> _logger;
        private readonly UserService _userService;
        private readonly BCryptService _bCryptService;
        private readonly JwtService _jwtService;
        private readonly UserNotificationOptionService _userNotificationOptionService;
        private readonly LeaguesService _leaguesService;
        private readonly LeagueParseListService _parserListService;
        private readonly ErrorsService _errorsService;
        private readonly NotificationsService _notificationsService;
        private readonly ParserService _parserService;
        private readonly UrlService _urlService;
        private readonly RedisService _redisService;

        private readonly DateTime _startDate = DateTime.UtcNow.AddDays(-4);
        public AuthController(ILogger<AuthController> logger, UserService userService, BCryptService bCryptService, JwtService jwtService, UserNotificationOptionService userNotificationOptionService, LeaguesService leaguesService, LeagueParseListService parserListService, ErrorsService errorsService, NotificationsService notificationsService, UrlService urlService, ParserService parserService, RedisService redisService)
        {
            _logger = logger;
            _userService = userService;
            _bCryptService = bCryptService;
            _jwtService = jwtService;
            _leaguesService = leaguesService;
            _userNotificationOptionService = userNotificationOptionService;
            _parserListService = parserListService;
            _errorsService = errorsService;
            _notificationsService = notificationsService;
            _urlService = urlService;
            _parserService = parserService;
            _redisService = redisService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
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

                user.PasswordHash = "";
                return Ok(user);
            }
            catch (Exception e)
            {
                _errorsService.CreateErrorFile(e);
                return StatusCode(500, e);
            }
        }


        [HttpPatch("exponotif/{newToken}")]
        public async Task<IActionResult> UpdateExpoNotif(string newToken)
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

                await _userService.UpdateExpoNotification(user.Id, newToken);

                return Ok();
            }
            catch (Exception e)
            {
                _errorsService.CreateErrorFile(e);
                return StatusCode(500, e);
            }
        }


        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
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

                var userNotification = await _notificationsService.GetNotificationByUserId(user.Id);

                return Ok(userNotification);
            }
            catch (Exception e)
            {
                _errorsService.CreateErrorFile(e);
                return StatusCode(500, e);
            }
        }


        [HttpGet("favorites")]
        public async Task<IActionResult> GetFavorites()
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

                var userNotificators = await _userNotificationOptionService.GetOptionsByUserId(user.Id);

                List<League> favoriteLeagues = await _leaguesService.GetManyByNames(userNotificators.Select(l => l.Name).ToList());

                return Ok(favoriteLeagues);
            }
            catch (Exception e)
            {
                _errorsService.CreateErrorFile(e);
                return StatusCode(500, e);
            }
        }

        [HttpGet("favorites/games")]
        public async Task<IActionResult> GetFavoriteGames(string? startDateString = null, string? endDateString = null)
        {
            try
            {
                DateTime? startDate = _startDate;
                DateTime? endDate = null;

                if (startDateString != null)
                {
                    startDate = DateTime.ParseExact(startDateString, "yyyy.MM.dd", CultureInfo.InvariantCulture);
                }

                if (endDateString != null)
                {
                    endDate = DateTime.ParseExact(endDateString, "yyyy.MM.dd", CultureInfo.InvariantCulture).AddHours(23).AddMinutes(59).AddSeconds(59);
                }

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

                var userNotificators = await _userNotificationOptionService.GetOptionsByUserId(user.Id);

                List<League> favoriteLeagues = await _leaguesService.GetManyByNames(userNotificators.Select(l => l.Name).ToList());

                Dictionary<string, GameWithLeague> gamesWithLeague = new();

                foreach (var favoriteLeague in favoriteLeagues)
                {
                    if (favoriteLeague == null)
                    {
                        continue;
                    }

                    List<Game> games = new();

                    var cachedData = await _redisService.GetCachedDataByUrl(favoriteLeague.Url);

                    if (cachedData == null)
                    {
                        games = await _parserService.GetGamesByUrl(favoriteLeague.Url, favoriteLeague.Title);
                    }
                    else
                    {
                        games = cachedData.Games;

                    }

                    foreach (var game in games)
                    {
                        GameWithLeague gameWithLeague = new()
                        {
                            League = favoriteLeague,
                            Game = game
                        };

                        gamesWithLeague[game.Url] = gameWithLeague;
                    }

                }

                var resData = gamesWithLeague.Values.ToList();

                resData.Sort((x, y) => x.Game.DateTime.CompareTo(y.Game.DateTime));

                return Ok(resData);
            }
            catch (Exception e)
            {
                _errorsService.CreateErrorFile(e);
                return StatusCode(500, e);
            }
        }




        [HttpPost("login")]
        public async Task<IActionResult> Login(AuthDto authDto)
        {
            try
            {
                var user = await _userService.GetByName(authDto.Login);

                if (user == null)
                {
                    user = await _userService.GetByPhone(authDto.Login);
                }

                if (user == null)
                {
                    user = await _userService.GetByEmail(authDto.Login);
                }

                if (user == null)
                {
                    return Unauthorized(new { Errors = new { Authorization = new string[] { "Неверный логин или пароль" } } });
                }

                bool isCorrectPassword = _bCryptService.Verify(authDto.Password, user.PasswordHash);

                if (!isCorrectPassword)
                {
                    return Unauthorized(new { Errors = new { Authorization = new string[] { "Неверный логин или пароль" } } });
                }

                if (!user.Active)
                {
                    return Unauthorized(new { Errors = new { Authorization = new string[] { "Аккаунт не активен. Возможно вы пропустили валидацию по номеру телефона" } } });
                }

                string token = _jwtService.GetUserToken(new UserTokenData()
                {
                    Id = user.Id,
                    Name = user.Name
                });

                return Ok(new { token });
            }
            catch (Exception e)
            {
                _errorsService.CreateErrorFile(e);
                return StatusCode(500, e);
            }

        }

        [HttpPost("signup")]
        public async Task<IActionResult> Create(CreateUserDto createUserDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                    return BadRequest(new { Errors = errors });
                }

                var dublicatedName = (await _userService.GetByName(createUserDto.Name)) != null;

                if (dublicatedName)
                {
                    return BadRequest(new { Errors = new { Authorization = new string[] { "Пользователь с таким именем уже существует" } } });
                }

                var dublicatedEmail = (await _userService.GetByEmail(createUserDto.Email)) != null;

                if (dublicatedEmail)
                {
                    return BadRequest(new { Errors = new { Authorization = new string[] { "Пользователь с таким E-mail уже существует" } } });
                }


                var dublicatedPhone = (await _userService.GetByPhone(createUserDto.Phone)) != null;

                if (dublicatedPhone)
                {
                    return BadRequest(new { Errors = new { Authorization = new string[] { "Пользователь с таким номером телефона уже существует" } } });
                }

                var uniquePhone = (await _userService.IsUniqueByPhone(createUserDto.UniqueId));

                if (!uniquePhone)
                {
                    return BadRequest(new { Errors = new { Authorization = new string[] { "Вы уже зарегистрировали аккаунт ранее" } } });
                }

                var userData = await _userService.Create(createUserDto);

                string token = _jwtService.GetUserToken(new UserTokenData()
                {
                    Id = userData.Id,
                    Name = userData.Name
                });

                return Ok(new { token });
            }
            catch (Exception e)
            {
                _errorsService.CreateErrorFile(e);
                return StatusCode(500, e);
            }
        }
    }
}
