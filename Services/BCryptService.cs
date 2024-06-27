using cardscore_api.Data;
using cardscore_api.Models.Dtos;

namespace cardscore_api.Services
{
    public class BCryptService
    {
        public BCryptService()
        {
        }

        public string Hash(string str)
        {
            string salt = BCrypt.Net.BCrypt.GenerateSalt();
            return BCrypt.Net.BCrypt.HashPassword(str, salt);
        }

        public bool Verify(string str, string hashedStr)
        {
            return BCrypt.Net.BCrypt.Verify(str, hashedStr);
        }
    }
}
