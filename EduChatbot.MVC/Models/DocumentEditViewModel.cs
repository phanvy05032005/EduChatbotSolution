using System.ComponentModel.DataAnnotations;

namespace EduChatbot.MVC.Models;

public class DocumentEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please enter the file name.")]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;
}
