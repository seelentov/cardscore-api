namespace cardscore_api.Models
{
    public class GameAction: GameActionBase
    {
        public Player Player { get; set; } = null!;
        public Player Player2 { get; set; } = null!;
    }

    public class GameActionBase
    {
        public bool LeftTeam { get; set; }
        public GameActionType? ActionType { get; set; } = null!;
        public string Time { get; set; } = null!;
    }

    public enum GameActionType
    {
        None,
        YellowCard,
        RedCard,
        YellowRedCard,
        Switch
    }
}
