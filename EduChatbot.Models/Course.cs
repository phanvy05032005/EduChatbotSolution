using System.ComponentModel.DataAnnotations;

namespace EduChatbot.Models;

public class Course
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<LecturerCourse> LecturerCourses { get; set; } = [];

    public List<Document> Documents { get; set; } = [];
}
