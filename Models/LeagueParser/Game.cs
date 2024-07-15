namespace cardscore_api.Models
{

    public class Game
    {
        public string Id { get; set; } = null!;
        public Team[] Teams {  get; set; } = null!;
        public List<GameAction> Actions {  get; set; } = null!;
        public string Url { get; set; } = null!;
        public int ActionsCount { get; set; } = 0;
        public DateTime DateTime { get; set; } = DateTime.UtcNow.AddHours(3);
        public bool ActiveGame { get; set; } = false;
        public bool FinishedToday { get; set; } = false;
        public string GameTime {  get; set; } = null!;
        public bool IsToday { get; set; } = false;
        public bool IsStopped { get; set; } = false;
    }
}
