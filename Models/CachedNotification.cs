namespace cardscore_api.Models
{
    public class CachedNotification
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public DateTime DateTime { get; set; }

        public CachedNotification(string key)
        {
            Key = key;
            DateTime = DateTime.UtcNow;
        }
    }
}
