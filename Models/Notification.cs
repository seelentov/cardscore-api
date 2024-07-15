namespace cardscore_api.Models
{
    public class Notification: GameActionBase
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string GameUrl { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string PlayerUrl { get; set; } = null!;
        public string PlayerName { get; set; } = null!;
        public string PlayerUrl2 { get; set; } = null!;
        public string PlayerName2 { get; set; } = null!;
        public DateTime DateTime { get; set; }
    }
}