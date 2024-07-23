using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace cardscore_api.Models
{
    public class League
    {
        public int Id { get; set; }
        public string Title {  get; set; } = null!;
        public string Country { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? GamesCount {  get; set; } = null!;
        public string Url { get; set; } = null!;
        public bool Active { get; set; } = true;

        public DateTime NearestGame { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    }
}
