using System.ComponentModel.DataAnnotations;

namespace cardscore_api.Models
{
    public class LeagueParseData
    {
        public int Id { get; set; }

        [Required]
        public string Url { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public ParserType ParserType { get; set; }
    }

    public enum ParserType
    {
        Soccer365,
        Soccerway
    }
}
