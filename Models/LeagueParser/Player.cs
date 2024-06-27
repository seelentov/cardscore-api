namespace cardscore_api.Models
{
    public class Player
    {
        public string Name { get; set; } = null!;
        public string Position { get; set; } = null!;
        public int? Goal { get; set; } = null!;
        public int? Assists { get; set; } = null!;
        public int? YellowCards {  get; set; } = null!;
        public int? RedCards {  get; set; } = null!;
        public int? YellowRedCards {  get; set; } = null!;
        public int? GameCount {  get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        public string Url {  get; set; } = null!;
        public bool DoubleParse {  get; set; } = false;

    }
}
