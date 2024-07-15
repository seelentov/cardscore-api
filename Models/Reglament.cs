using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace cardscore_api.Models
{
    public class Reglament
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = null!;
        [Required]
        public string Text { get; set; } = null!;
        public bool Active { get; set; } = true;
    }
}
