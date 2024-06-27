using System.ComponentModel.DataAnnotations;

namespace cardscore_api.Models.Dtos
{
    public class EditInfoDto
    {
        [Required]
        public string Description { get; set; }
    }
}
