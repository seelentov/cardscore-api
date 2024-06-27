using cardscore_api.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace cardscore_api.Services
{
    public class JwtService
    {
        private readonly string _jwtKey;
        public JwtService(IConfiguration configuration)
        {
            _jwtKey = configuration["Jwt:Key"]!;
        }

        public string GetUserToken(UserTokenData userTokenData)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim("Id", userTokenData.Id.ToString()),
                new Claim("Name", userTokenData.Name)
            }),
                Expires = DateTime.UtcNow.AddDays(365),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey)), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public UserTokenData DecodeUserToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey)),
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateLifetime = true
            };
            SecurityToken validatedToken;
            var principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
            var userTokenData = new UserTokenData();
            userTokenData.Id = int.Parse(principal.FindFirst("Id").Value);
            userTokenData.Name = principal.FindFirst("Name").Value;
            return userTokenData;
        }
    }
}
