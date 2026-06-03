using EduChatbot.Models;
using EduChatbot.Models.Identity;

namespace EduChatbot.MVC.Models;

public class AdminCoursesViewModel
{
    public List<Course> Courses { get; set; } = [];
    public List<ApplicationUser> Lecturers { get; set; } = [];
    public string? NewCourseCode { get; set; }
    public string? NewCourseName { get; set; }
}
