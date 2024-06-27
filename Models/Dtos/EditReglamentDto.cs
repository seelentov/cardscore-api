using System.ComponentModel.DataAnnotations;

namespace cardscore_api.Models.Dtos
{
    public class EditReglamentDto
    {
        [Required]
        public string Text { get; set; }
    }
}
