using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace cardscore_api.Models
{
    public class LeagueIncludeGames
    {
        public int Id { get; set; }
        public string Title {  get; set; } = null!;
        public string Country { get; set; } = null!;

        public List<Game> Games { get; set; } = new List<Game>();
        public int? GamesCount { get; set; } = null!;
        public DateTime? StartDate { get; set; } = null!;
        public DateTime? EndDate { get; set; } = null!;
        public string Url { get; set; } = null!;
        public bool Active { get; set; } = true;
    }
}
