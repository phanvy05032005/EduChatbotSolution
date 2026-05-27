using System.ComponentModel.DataAnnotations;

namespace EduChatbot.MVC.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "Student";
}
