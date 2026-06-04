using System.ComponentModel.DataAnnotations;
using EduChatbot.Models.Identity;

namespace EduChatbot.Models;

public class LecturerCourse
{
    [Required]
    [MaxLength(450)]
    public string LecturerId { get; set; } = string.Empty;

    public ApplicationUser? Lecturer { get; set; }

    [Required]
    public int CourseId { get; set; }

    public Course? Course { get; set; }
}
