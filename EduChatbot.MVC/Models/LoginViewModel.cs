using System.ComponentModel.DataAnnotations;

namespace EduChatbot.MVC.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
