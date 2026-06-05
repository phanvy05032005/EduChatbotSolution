using EduChatbot.Models;
using EduChatbot.Models.Identity;

namespace EduChatbot.Business.Services;

public interface IAdminService
{
    Task<AdminStatisticsInfo> GetStatisticsAsync();

    Task<List<AdminAccountInfo>> GetAccountsByRoleAsync(string role, string? searchTerm = null);

    Task<AdminAccountEditInfo?> GetAccountForEditAsync(string id);

    Task<AdminOperationResult> CreateAccountAsync(string fullName, string email, string password, string role, bool sendEmail = true, List<int>? courseIds = null);

    Task<AdminOperationResult> UpdateAccountAsync(string id, string fullName, string email);

    Task<AdminOperationResult> LockAccountAsync(string id);

    Task<AdminOperationResult> UnlockAccountAsync(string id);

    Task<AdminOperationResult> DeleteAccountAsync(string id, string currentUserId);

    Task<bool> CanConnectToDatabaseAsync();

    Task<AdminOperationResult> ImportStudentsFromExcelAsync(Stream fileStream, bool sendEmail = true);

    Task<AdminOperationResult> ImportLecturersFromExcelAsync(Stream fileStream, bool sendEmail = true);

    Task<List<Course>> GetCoursesAsync();

    Task<Course?> GetCourseByIdAsync(int id);

    Task<AdminOperationResult> CreateCourseAsync(string code, string name, string description);

    Task<AdminOperationResult> DeleteCourseAsync(int id);

    Task<List<ApplicationUser>> GetLecturersAsync();

    Task<List<Course>> GetLecturerCoursesAsync(string lecturerId);

    Task<AdminOperationResult> AssignLecturerToCourseAsync(string lecturerId, int courseId);

    Task<AdminOperationResult> RemoveLecturerFromCourseAsync(string lecturerId, int courseId);

    Task<AdminOperationResult> ImportCoursesFromExcelAsync(Stream fileStream);
}
