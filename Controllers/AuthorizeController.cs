using cardscore_api.Models;
using cardscore_api.Models.Dtos;
using cardscore_api.Models.LeagueParser;
using cardscore_api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

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

        private readonly DateTime _startDate = DateTime.UtcNow.AddDays(-7);
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
                    return Unauthorized(new { Message = "\u0417\u0430\u0433\u043e\u043b\u043e\u0432\u043e\u043a\u0020\u0041\u0075\u0074\u0068\u006f\u0072\u0069\u007a\u0061\u0074\u0069\u006f\u006e\u0020\u043f\u0443\u0441\u0442", userId });
                }

                var user = await _userService.GetById(int.Parse(userId));

                if (user == null)
                {
                    return Unauthorized(new { Message = "\u041f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u0442\u0435\u043b\u044c\u0020\u043d\u0435\u0020\u043d\u0430\u0439\u0434\u0435\u043d", userId });
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
                    return Unauthorized(new { Message = "\u0417\u0430\u0433\u043e\u043b\u043e\u0432\u043e\u043a\u0020\u0041\u0075\u0074\u0068\u006f\u0072\u0069\u007a\u0061\u0074\u0069\u006f\u006e\u0020\u043f\u0443\u0441\u0442", userId });
                }

                var user = await _userService.GetById(int.Parse(userId));

                if (user == null)
                {
                    return Unauthorized(new { Message = "\u041f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u0442\u0435\u043b\u044c\u0020\u043d\u0435\u0020\u043d\u0430\u0439\u0434\u0435\u043d", userId });
                }

                await _userService.ClearSameExpo(newToken, user.Id);

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
                    return Unauthorized(new { Message = "\u0417\u0430\u0433\u043e\u043b\u043e\u0432\u043e\u043a\u0020\u0041\u0075\u0074\u0068\u006f\u0072\u0069\u007a\u0061\u0074\u0069\u006f\u006e\u0020\u043f\u0443\u0441\u0442", userId });
                }

                var user = await _userService.GetById(int.Parse(userId));

                if (user == null)
                {
                    return Unauthorized(new { Message = "\u041f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u0442\u0435\u043b\u044c\u0020\u043d\u0435\u0020\u043d\u0430\u0439\u0434\u0435\u043d", userId });
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
                    return Unauthorized(new { Message = "\u0417\u0430\u0433\u043e\u043b\u043e\u0432\u043e\u043a\u0020\u0041\u0075\u0074\u0068\u006f\u0072\u0069\u007a\u0061\u0074\u0069\u006f\u006e\u0020\u043f\u0443\u0441\u0442", userId });
                }

                var user = await _userService.GetById(int.Parse(userId));

                if (user == null)
                {
                    return Unauthorized(new { Message = "\u041f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u0442\u0435\u043b\u044c\u0020\u043d\u0435\u0020\u043d\u0430\u0439\u0434\u0435\u043d", userId });
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
                    return Unauthorized(new { Message = "\u0417\u0430\u0433\u043e\u043b\u043e\u0432\u043e\u043a\u0020\u0041\u0075\u0074\u0068\u006f\u0072\u0069\u007a\u0061\u0074\u0069\u006f\u006e\u0020\u043f\u0443\u0441\u0442", userId });
                }

                var user = await _userService.GetById(int.Parse(userId));

                if (user == null)
                {
                    return Unauthorized(new { Message = "\u041f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u0442\u0435\u043b\u044c\u0020\u043d\u0435\u0020\u043d\u0430\u0439\u0434\u0435\u043d", userId });
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

                    var cachedData = await _redisService.GetCachedDataByUrl(favoriteLeague.Url, DateTime.UtcNow.AddDays(-7));

                    if (cachedData == null)
                    {
                        games = new List<Game>();
                        Console.WriteLine("Fav Empty: " + favoriteLeague.Title);
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
                    return Unauthorized(new { Errors = new { Authorization = new string[] { "\u041d\u0435\u0432\u0435\u0440\u043d\u044b\u0439\u0020\u043b\u043e\u0433\u0438\u043d\u0020\u0438\u043b\u0438\u0020\u043f\u0430\u0440\u043e\u043b\u044c" } } });
                }

                bool isCorrectPassword = _bCryptService.Verify(authDto.Password, user.PasswordHash);

                if (!isCorrectPassword)
                {
                    return Unauthorized(new { Errors = new { Authorization = new string[] { "\u041d\u0435\u0432\u0435\u0440\u043d\u044b\u0439\u0020\u043b\u043e\u0433\u0438\u043d\u0020\u0438\u043b\u0438\u0020\u043f\u0430\u0440\u043e\u043b\u044c" } } });
                }

                if (!user.Active)
                {
                    return Unauthorized(new { Errors = new { Authorization = new string[] { "\u0410\u043a\u043a\u0430\u0443\u043d\u0442\u0020\u043d\u0435\u0020\u0430\u043a\u0442\u0438\u0432\u0435\u043d" } } });
                }

                await _userService.ClearSameExpo(user.ExpoToken, user.Id);

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
                    return BadRequest(new { Errors = new { Authorization = new string[] { "\u041f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u0442\u0435\u043b\u044c\u0020\u0441\u0020\u0442\u0430\u043a\u0438\u043c\u0020\u0438\u043c\u0435\u043d\u0435\u043c\u0020\u0443\u0436\u0435\u0020\u0441\u0443\u0449\u0435\u0441\u0442\u0432\u0443\u0435\u0442" } } });
                }

                var dublicatedEmail = (await _userService.GetByEmail(createUserDto.Email)) != null;

                if (dublicatedEmail)
                {
                    return BadRequest(new { Errors = new { Authorization = new string[] { "\u041f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u0442\u0435\u043b\u044c\u0020\u0441\u0020\u0442\u0430\u043a\u0438\u043c\u0020\u0045\u002d\u006d\u0061\u0069\u006c\u0020\u0443\u0436\u0435\u0020\u0441\u0443\u0449\u0435\u0441\u0442\u0432\u0443\u0435\u0442" } } });
                }


                var dublicatedPhone = (await _userService.GetByPhone(createUserDto.Phone)) != null;

                if (dublicatedPhone)
                {
                    return BadRequest(new { Errors = new { Authorization = new string[] { "\u041f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u0442\u0435\u043b\u044c\u0020\u0441\u0020\u0442\u0430\u043a\u0438\u043c\u0020\u043d\u043e\u043c\u0435\u0440\u043e\u043c\u0020\u0442\u0435\u043b\u0435\u0444\u043e\u043d\u0430\u0020\u0443\u0436\u0435\u0020\u0441\u0443\u0449\u0435\u0441\u0442\u0432\u0443\u0435\u0442" } } });
                }

                var uniquePhone = (await _userService.IsUniqueByPhone(createUserDto.UniqueId));

                if (!uniquePhone)
                {
                    return BadRequest(new { Errors = new { Authorization = new string[] { "\u0412\u044b\u0020\u0443\u0436\u0435\u0020\u0437\u0430\u0440\u0435\u0433\u0438\u0441\u0442\u0440\u0438\u0440\u043e\u0432\u0430\u043b\u0438\u0020\u0430\u043a\u043a\u0430\u0443\u043d\u0442\u0020\u0440\u0430\u043d\u0435\u0435" } } });
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
