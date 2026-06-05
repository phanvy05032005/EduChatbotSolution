using System.ComponentModel.DataAnnotations;

namespace EduChatbot.MVC.Models;

public class AccountProfileViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
