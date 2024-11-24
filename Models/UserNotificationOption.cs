using System.Text.Json.Serialization;

namespace cardscore_api.Models
{
    public class UserNotificationOption
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public string Name { get; set; }
        public int CardCount { get; set; } = 1;
        public int CardCountTwo { get; set; } = 2;
        public int CardCountThree { get; set; } = 3;
        public int CardCountFour { get; set; } = 4;

        public bool Active { get; set; } = true;
        public UserNotificationOptionType UserNotificationOptionType { get; set; } = UserNotificationOptionType.YellowCard;
    }

    public class UserNotificationOptionResponse
    {
        public string Name { get; set; }
        public int CardCount { get; set; } = 1;
        public int CardCountTwo { get; set; } = 2;
        public int CardCountThree { get; set; } = 3;
        public int CardCountFour { get; set; } = 3;
        public bool Active { get; set; }

    }

    public enum UserNotificationOptionType
    {
        YellowCard
    }
}
