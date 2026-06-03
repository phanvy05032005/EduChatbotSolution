using System.ComponentModel.DataAnnotations;

namespace EduChatbot.MVC.Models;

public class AdminAccountFormViewModel
{
    public string? Id { get; set; }

    [Required]
    public string AccountType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the full name.")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the email.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    public string? Password { get; set; }

    public bool SendEmail { get; set; } = false;

    public bool IsEdit => !string.IsNullOrWhiteSpace(Id);
}
