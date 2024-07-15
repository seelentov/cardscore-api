using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Io;
using cardscore_api.Data;
using cardscore_api.Models;
using cardscore_api.Services;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium;
using System.Net;

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
        private readonly DataContext _context;
        private readonly ParserService _parserService;
        private readonly FetchService _fetchService;
        public TestController(LeaguesService leaguesService, IHostEnvironment environment, UserService userService, ExpoNotificationsService expoNotificationsService, NotificationsService notificationsService, DataContext context, ParserService parserService, FetchService fetchService)
        {
            _leaguesService = leaguesService;
            _environment = environment;
            _userService = userService;
            _expoNotificationsService = expoNotificationsService;
            _notificationsService = notificationsService;
            _context = context;
            _parserService = parserService;
            _fetchService = fetchService;
        }

        [HttpGet("parse")]
        public async Task<IActionResult> Parse()
        {
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

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> Test()
        {
            var _web = new HtmlWeb();
            _web.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:127.0) Gecko/20100101 Firefox/127.0";

            var url1 = "https://ru.soccerway.com/national/norway/eliteserien/2024/regular-season/r79902/";

            var doc = _web.Load(url1);

            return Ok(doc.DocumentNode.InnerHtml);
        }
        [HttpGet("2")]
        public async Task<IActionResult> Test2()
        {
            var url1 = "https://ru.soccerway.com/national/korea-republic/k-league-classic/2024/regular-season/r79835/";
            var data = await _parserService.GetActiveGamesByUrl(url1);

            return Ok(data);
        }
    }
}
