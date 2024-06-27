using System.ComponentModel.DataAnnotations;

namespace cardscore_api.Models.Dtos
{
    public class EditUserNotificationOptionsDto
    {
        [Required]
        public List<EditUserNotificationOption> Options;
    }

    public struct EditUserNotificationOption
    {
        public string Name { get; set; }

        public int CardCount { get; set; }
        public int CardCountTwo { get; set; }
        public int CardCountThree { get; set; }
        public bool Active { get; set; }
    }

}
