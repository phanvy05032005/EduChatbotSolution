using EduChatbot.Data;
using EduChatbot.Models;
using EduChatbot.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniExcelLibs;
using System.IO;

namespace EduChatbot.Business.Services;

public class AdminService : IAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public AdminService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IEmailService emailService)
    {
        _userManager = userManager;
        _context = context;
        _emailService = emailService;
    }

    public async Task<AdminStatisticsInfo> GetStatisticsAsync()
    {
        return new AdminStatisticsInfo
        {
            TotalStudents = (await _userManager.GetUsersInRoleAsync(ApplicationRoles.Student)).Count,
            TotalLecturers = (await _userManager.GetUsersInRoleAsync(ApplicationRoles.Lecturer)).Count,
            TotalDocuments = await _context.Documents.CountAsync(),
            TotalQuestionsAsked = await _context.ChatMessages.CountAsync(message => message.Role == "user")
        };
    }

    public async Task<List<AdminAccountInfo>> GetAccountsByRoleAsync(string role, string? searchTerm = null)
    {
        var users = await _userManager.GetUsersInRoleAsync(role);
        var query = users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var keyword = searchTerm.Trim().ToLowerInvariant();
            query = query.Where(user =>
                user.Id.ToLowerInvariant().Contains(keyword) ||
                user.FullName.ToLowerInvariant().Contains(keyword) ||
                (user.Email ?? string.Empty).ToLowerInvariant().Contains(keyword));
        }

        return query
            .OrderBy(user => user.Email)
            .Select(user => new AdminAccountInfo
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Department = role == ApplicationRoles.Lecturer ? "General Department" : "N/A",
                Status = IsLocked(user) ? "Locked" : "Active"
            })
            .ToList();
    }

    public async Task<AdminAccountEditInfo?> GetAccountForEditAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new AdminAccountEditInfo
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = roles.FirstOrDefault() ?? string.Empty
        };
    }

    public async Task<AdminOperationResult> CreateAccountAsync(string fullName, string email, string password, string role, bool sendEmail = false)
    {
        if (!IsManageableRole(role))
        {
            return Failure("Invalid account role.");
        }

        var user = new ApplicationUser
        {
            UserName = email.Trim(),
            Email = email.Trim(),
            FullName = fullName.Trim(),
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return Failure(string.Join(" ", result.Errors.Select(error => error.Description)));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return Failure(string.Join(" ", roleResult.Errors.Select(error => error.Description)));
        }

        if (sendEmail)
        {
            try
            {
                var subject = "[EduChatbot] Thông tin tài khoản mới";
                var body = $@"Xin chào {fullName.Trim()},

Tài khoản {role.ToLower()} của bạn đã được tạo trên hệ thống EduChatbot bởi Quản trị viên.

Thông tin đăng nhập của bạn:
- Email đăng nhập: {email.Trim()}
- Mật khẩu: {password}

Trân trọng,
Hệ thống EduChatbot";

                await _emailService.SendEmailAsync(email.Trim(), subject, body);
            }
            catch
            {
                // Không chặn quá trình tạo tài khoản nếu email lỗi
            }
        }

        return Success($"{role} account created successfully.");
    }

    public async Task<AdminOperationResult> UpdateAccountAsync(string id, string fullName, string email)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return Failure("Account not found.");
        }

        user.FullName = fullName.Trim();
        user.Email = email.Trim();
        user.UserName = email.Trim();

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded
            ? Success("Account updated successfully.")
            : Failure(string.Join(" ", result.Errors.Select(error => error.Description)));
    }

    public async Task<AdminOperationResult> LockAccountAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return Failure("Account not found.");
        }

        await _userManager.SetLockoutEnabledAsync(user, true);
        var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));

        return result.Succeeded
            ? Success("Account locked successfully.")
            : Failure(string.Join(" ", result.Errors.Select(error => error.Description)));
    }

    public async Task<AdminOperationResult> UnlockAccountAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return Failure("Account not found.");
        }

        var result = await _userManager.SetLockoutEndDateAsync(user, null);
        return result.Succeeded
            ? Success("Account unlocked successfully.")
            : Failure(string.Join(" ", result.Errors.Select(error => error.Description)));
    }

    public async Task<AdminOperationResult> DeleteAccountAsync(string id, string currentUserId)
    {
        if (id == currentUserId)
        {
            return Failure("You cannot delete your own admin account.");
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return Failure("Account not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains(ApplicationRoles.Admin))
        {
            return Failure("Admin accounts cannot be deleted from this page.");
        }

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded
            ? Success("Account deleted successfully.")
            : Failure(string.Join(" ", result.Errors.Select(error => error.Description)));
    }

    public Task<bool> CanConnectToDatabaseAsync()
    {
        return _context.Database.CanConnectAsync();
    }

    private static bool IsLocked(ApplicationUser user)
    {
        return user.LockoutEnd.HasValue && user.LockoutEnd.Value.UtcDateTime > DateTime.UtcNow;
    }

    private static bool IsManageableRole(string role)
    {
        return role == ApplicationRoles.Student || role == ApplicationRoles.Lecturer;
    }

    private static AdminOperationResult Success(string message)
    {
        return new AdminOperationResult { IsSuccess = true, Message = message };
    }

    private static AdminOperationResult Failure(string message)
    {
        return new AdminOperationResult { IsSuccess = false, Message = message };
    }

    public async Task<AdminOperationResult> ImportStudentsFromExcelAsync(Stream fileStream, bool sendEmail = false)
    {
        try
        {
            var rows = MiniExcel.Query(fileStream).ToList();
            if (rows.Count <= 1)
            {
                return Failure("File Excel không có dữ liệu hoặc trống.");
            }

            var header = rows[0] as IDictionary<string, object>;
            if (header == null) return Failure("Định dạng file Excel không hợp lệ.");

            string emailKey = "";
            string nameKey = "";

            foreach (var key in header.Keys)
            {
                var val = header[key]?.ToString()?.ToLowerInvariant() ?? "";
                if (val.Contains("email") || val.Contains("thư điện tử")) emailKey = key;
                else if (val.Contains("name") || val.Contains("tên") || val.Contains("họ tên")) nameKey = key;
            }

            if (string.IsNullOrEmpty(emailKey) || string.IsNullOrEmpty(nameKey))
            {
                var keys = header.Keys.ToList();
                if (keys.Count >= 2)
                {
                    emailKey = keys[0];
                    nameKey = keys[1];
                }
                else
                {
                    return Failure("File Excel cần ít nhất 2 cột: Email và Họ tên.");
                }
            }

            int successCount = 0;
            int failedCount = 0;
            var errorMessages = new List<string>();

            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i] as IDictionary<string, object>;
                if (row == null) continue;

                var email = row[emailKey]?.ToString()?.Trim();
                var fullName = row[nameKey]?.ToString()?.Trim();

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(fullName))
                {
                    failedCount++;
                    errorMessages.Add($"Dòng {i + 1}: Thiếu Email hoặc Họ tên.");
                    continue;
                }

                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    failedCount++;
                    errorMessages.Add($"Dòng {i + 1} ({email}): Email đã tồn tại.");
                    continue;
                }

                var randomPassword = GenerateRandomPassword();
                
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(user, randomPassword);
                if (!createResult.Succeeded)
                {
                    failedCount++;
                    errorMessages.Add($"Dòng {i + 1} ({email}): {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                    continue;
                }

                var roleResult = await _userManager.AddToRoleAsync(user, ApplicationRoles.Student);
                if (!roleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    failedCount++;
                    errorMessages.Add($"Dòng {i + 1} ({email}): Không thể gán vai trò.");
                    continue;
                }

                if (sendEmail)
                {
                    try
                    {
                        var subject = "[EduChatbot] Thông tin tài khoản học tập mới";
                        var body = $@"Xin chào {fullName},

Tài khoản sinh viên của bạn đã được tạo trên hệ thống EduChatbot bởi Quản trị viên.

Thông tin đăng nhập của bạn:
- Email đăng nhập: {email}
- Mật khẩu: {randomPassword}

Vui lòng đăng nhập và thay đổi mật khẩu trong lần đầu tiên sử dụng.

Trân trọng,
Hệ thống EduChatbot";

                        await _emailService.SendEmailAsync(email, subject, body);
                    }
                    catch
                    {
                        // Không chặn import nếu email lỗi
                    }
                }
                successCount++;
            }

            var msg = $"Nhập danh sách thành công {successCount} sinh viên.";
            if (failedCount > 0)
            {
                msg += $" Thất bại {failedCount} dòng. Chi tiết: {string.Join("; ", errorMessages.Take(5))}";
                if (errorMessages.Count > 5) msg += "...";
            }

            return new AdminOperationResult { IsSuccess = successCount > 0, Message = msg };
        }
        catch (Exception ex)
        {
            return Failure($"Lỗi khi xử lý file Excel: {ex.Message}");
        }
    }

    private static string GenerateRandomPassword()
    {
        var upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var lower = "abcdefghijklmnopqrstuvwxyz";
        var digits = "0123456789";
        var special = "@#$!%*?&";
        var random = new Random();

        var pwd = new char[8];
        pwd[0] = upper[random.Next(upper.Length)];
        pwd[1] = lower[random.Next(lower.Length)];
        pwd[2] = digits[random.Next(digits.Length)];
        pwd[3] = special[random.Next(special.Length)];

        var all = upper + lower + digits + special;
        for (int i = 4; i < 8; i++)
        {
            pwd[i] = all[random.Next(all.Length)];
        }

        return new string(pwd.OrderBy(_ => random.Next()).ToArray());
    }

    public async Task<List<Course>> GetCoursesAsync()
    {
        return await _context.Courses
            .Include(c => c.LecturerCourses)
                .ThenInclude(lc => lc.Lecturer)
            .OrderBy(c => c.Code)
            .ToListAsync();
    }

    public async Task<Course?> GetCourseByIdAsync(int id)
    {
        return await _context.Courses
            .Include(c => c.LecturerCourses)
                .ThenInclude(lc => lc.Lecturer)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<AdminOperationResult> CreateCourseAsync(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            return Failure("Mã môn học và tên môn học không được để trống.");
        }

        var normalizedCode = code.Trim().ToUpperInvariant();
        var existing = await _context.Courses.AnyAsync(c => c.Code == normalizedCode);
        if (existing)
        {
            return Failure($"Môn học có mã '{normalizedCode}' đã tồn tại.");
        }

        var course = new Course
        {
            Code = normalizedCode,
            Name = name.Trim()
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        return Success("Tạo môn học thành công.");
    }

    public async Task<AdminOperationResult> DeleteCourseAsync(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return Failure("Không tìm thấy môn học.");
        }

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();

        return Success("Xóa môn học thành công.");
    }

    public async Task<List<ApplicationUser>> GetLecturersAsync()
    {
        return (await _userManager.GetUsersInRoleAsync(ApplicationRoles.Lecturer)).ToList();
    }

    public async Task<List<Course>> GetLecturerCoursesAsync(string lecturerId)
    {
        return await _context.LecturerCourses
            .Where(lc => lc.LecturerId == lecturerId)
            .Select(lc => lc.Course!)
            .OrderBy(c => c.Code)
            .ToListAsync();
    }

    public async Task<AdminOperationResult> AssignLecturerToCourseAsync(string lecturerId, int courseId)
    {
        var lecturer = await _userManager.FindByIdAsync(lecturerId);
        if (lecturer == null) return Failure("Không tìm thấy giảng viên.");

        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return Failure("Không tìm thấy môn học.");

        var isLecturer = await _userManager.IsInRoleAsync(lecturer, ApplicationRoles.Lecturer);
        if (!isLecturer) return Failure("Người dùng không phải giảng viên.");

        var existing = await _context.LecturerCourses
            .AnyAsync(lc => lc.LecturerId == lecturerId && lc.CourseId == courseId);
        if (existing)
        {
            return Success("Giảng viên đã được phân công dạy môn này.");
        }

        var assignment = new LecturerCourse
        {
            LecturerId = lecturerId,
            CourseId = courseId
        };

        _context.LecturerCourses.Add(assignment);
        await _context.SaveChangesAsync();

        return Success("Phân công giảng viên cho môn học thành công.");
    }

    public async Task<AdminOperationResult> RemoveLecturerFromCourseAsync(string lecturerId, int courseId)
    {
        var assignment = await _context.LecturerCourses
            .FirstOrDefaultAsync(lc => lc.LecturerId == lecturerId && lc.CourseId == courseId);
        if (assignment == null)
        {
            return Failure("Không tìm thấy phân công dạy môn này.");
        }

        _context.LecturerCourses.Remove(assignment);
        await _context.SaveChangesAsync();

        return Success("Hủy phân công dạy môn học thành công.");
    }
}
