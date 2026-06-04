using EduChatbot.Models;
using Microsoft.EntityFrameworkCore;

namespace EduChatbot.Data.Repositories;

public class CourseRepository : ICourseRepository
{
    private readonly ApplicationDbContext _context;

    public CourseRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Course?> GetByIdAsync(int id)
    {
        return await _context.Courses.FirstOrDefaultAsync(course => course.Id == id);
    }

    public async Task<List<Course>> GetAllAsync()
    {
        return await _context.Courses
            .OrderBy(course => course.Code)
            .ToListAsync();
    }

    public async Task<List<Course>> GetAssignedCoursesAsync(string lecturerId)
    {
        return await _context.LecturerCourses
            .Where(assignment => assignment.LecturerId == lecturerId)
            .Select(assignment => assignment.Course!)
            .OrderBy(course => course.Code)
            .ToListAsync();
    }

    public async Task<bool> IsLecturerAssignedToCourseAsync(string lecturerId, int courseId)
    {
        // Business layer dùng method này để chặn lecturer upload môn chưa được phân công.
        return await _context.LecturerCourses.AnyAsync(assignment =>
            assignment.LecturerId == lecturerId &&
            assignment.CourseId == courseId);
    }
}
