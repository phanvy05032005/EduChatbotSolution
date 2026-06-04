using EduChatbot.Models;

namespace EduChatbot.Data.Repositories;

public interface ICourseRepository
{
    Task<Course?> GetByIdAsync(int id);

    Task<List<Course>> GetAllAsync();

    Task<List<Course>> GetAssignedCoursesAsync(string lecturerId);

    Task<bool> IsLecturerAssignedToCourseAsync(string lecturerId, int courseId);
}
