using System.ComponentModel.DataAnnotations;
using EduChatbot.Models;

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

    public List<int> SelectedCourseIds { get; set; } = [];

    public List<Course> AvailableCourses { get; set; } = [];

    public bool IsEdit => !string.IsNullOrWhiteSpace(Id);
}
