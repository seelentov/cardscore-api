using cardscore_api.Models;
using cardscore_api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace cardscore_api.Middlewares
{
    public class JwtMiddleware
    {

        private readonly RequestDelegate _next;
        private readonly JwtService _jwtService;
        public JwtMiddleware(RequestDelegate next, JwtService jwtService)
        {
            _next = next;
            _jwtService = jwtService;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            string token = context.Request.Headers["Authorization"].FirstOrDefault();

            if(!string.IsNullOrEmpty(token))
            {
                try
                {
                    UserTokenData decodedToken = _jwtService.DecodeUserToken(token);
                    context.Request.Headers.Append("UserId", decodedToken.Id.ToString());
                }
                catch
                {
                    await _next(context);
                }
                
            }

            await _next(context);
        }
    }
}
