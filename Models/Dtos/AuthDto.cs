using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cardscore_api.Models.Dtos
{
    public class AuthDto
    {
        public string Login { get; set; }

        public string Password { get; set; }
    }
}
