namespace cardscore_api.Models
{
    public class Info
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; } = true;
    }
}
