using System.ComponentModel.DataAnnotations;

namespace cardscore_api.Models.Dtos
{
    public class CreateUserDto
    {
        [Required(ErrorMessage = "Введите имя ")]
        [Length(2, 20, ErrorMessage = "Длина имени должна быть от 2 символов до 20")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "Номер телефона - обязателен")]
        [Phone(ErrorMessage = "Введите корректный номер телефона")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Email - обязателен")]
        [EmailAddress(ErrorMessage = "Введите корректный E-mail")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Введите пароль")]
        [Length(8, 20, ErrorMessage = "Длина пароля должна быть от 8 символов до 20")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Должен быть уникальный идентификатор")]
        public string UniqueId {  get; set; }

    }
}
