using cardscore_api.Models;
using cardscore_api.Services;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace cardscore_api.Middlewares
{
    public class BasicAuthMiddleware
    {

        private readonly RequestDelegate _next;
        private readonly string _basicKey;
        public BasicAuthMiddleware(IConfiguration configuration, RequestDelegate next)
        {
            _next = next;
            _basicKey = configuration["Basic:Key"]!;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            var isApiReq = context.Request.Path.Value.Contains("api");

            string basicKey = context.Request.Headers["ApiKey"].FirstOrDefault();

            if (isApiReq && (basicKey == null || basicKey != _basicKey))
            {
                context.Response.StatusCode = 401;
                return;
            }

            await _next(context);
        }
    }
}
