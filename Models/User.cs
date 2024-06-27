using System.Text.Json.Serialization;

namespace cardscore_api.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Phone {  get; set; }
        public string Email {  get; set; }
        public string PasswordHash { get; set; }
        public List<League> Favorites {  get; set; }
        public bool Active { get; set; }
        public string UniqueId { get; set; }
        public List<UserNotificationOption> Options {  get; set; }
        public int RoleId { get; set; }
        public Role Role { get; set; }
        public DateTime SubData { get; set; }
        public SubStatus SubStatus { get; set; }
        public string ExpoToken { get; set; } = "";

    }

    public enum SubStatus
    {
        Test = 0, 
        Payed = 1
    }
}
