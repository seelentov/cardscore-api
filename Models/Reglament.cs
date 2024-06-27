using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace cardscore_api.Models
{
    public class Reglament
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = null!;
        [Required]
        public string Text { get; set; } = null!;

        public int LeagueId { get; set; }
        public League League { get; set; }
        public bool Active { get; set; } = true;
    }
}
