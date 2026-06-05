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
    private readonly IEmailQueueService _emailQueueService;

    public AdminService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IEmailService emailService,
        IEmailQueueService emailQueueService)
    {
        _userManager = userManager;
        _context = context;
        _emailService = emailService;
        _emailQueueService = emailQueueService;
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

    public async Task<AdminOperationResult> CreateAccountAsync(string fullName, string email, string password, string role, bool sendEmail = true, List<int>? courseIds = null)
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

        // Assign lecturer to teach selected courses
        var assignedCourseNames = new List<string>();
        if (role == ApplicationRoles.Lecturer && courseIds != null && courseIds.Count > 0)
        {
            foreach (var courseId in courseIds)
            {
                var course = await _context.Courses.FindAsync(courseId);
                if (course != null)
                {
                    assignedCourseNames.Add($"{course.Code} - {course.Name}");
                    var existing = await _context.LecturerCourses
                        .AnyAsync(lc => lc.LecturerId == user.Id && lc.CourseId == courseId);
                    if (!existing)
                    {
                        _context.LecturerCourses.Add(new LecturerCourse
                        {
                            LecturerId = user.Id,
                            CourseId = courseId
                        });
                    }
                }
            }
            await _context.SaveChangesAsync();
        }

        var emailQueued = false;
        if (sendEmail)
        {
            try
            {
                var subject = "[EduChatbot] New Account Credentials";
                var courseInfo = assignedCourseNames.Count > 0
                    ? $"\n- Assigned Courses: {string.Join(", ", assignedCourseNames)}"
                    : "";

                var body = $@"Hello {fullName.Trim()},

Your {role.ToLower()} account has been created on the EduChatbot system by the Administrator.

Your login credentials:
- Login Email: {email.Trim()}
- Password: {password}{courseInfo}

Best regards,
EduChatbot System";

                // IMPORTANT: Do not send email directly -> push to queue for background worker sending.
                await _emailQueueService.EnqueueAsync(email.Trim(), subject, body);
                emailQueued = true;
            }
            catch
            {
                // Do not block account creation if email enqueue fails
                emailQueued = false;
            }
        }

        var successMessage = sendEmail
            ? (emailQueued
                ? $"{role} account created successfully and email notification was queued."
                : $"{role} account created successfully, but email notification could not be queued.")
            : $"{role} account created successfully.";

        return Success(successMessage);
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

    public async Task<AdminOperationResult> ImportStudentsFromExcelAsync(Stream fileStream, bool sendEmail = true)
    {
        try
        {
            var rows = MiniExcel.Query(fileStream).ToList();
            if (rows.Count <= 1)
            {
                return Failure("The Excel file has no data or is empty.");
            }

            var header = rows[0] as IDictionary<string, object>;
            if (header == null) return Failure("Invalid Excel file format.");

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
                    return Failure("Excel file requires at least 2 columns: Email and FullName.");
                }
            }

            int successCount = 0;
            int failedCount = 0;
            int emailQueuedCount = 0;
            int emailQueueFailedCount = 0;
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
                    errorMessages.Add($"Row {i + 1}: Missing Email or FullName.");
                    continue;
                }

                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    failedCount++;
                    errorMessages.Add($"Row {i + 1} ({email}): Email already exists.");
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
                    errorMessages.Add($"Row {i + 1} ({email}): Cannot assign role.");
                    continue;
                }

                if (sendEmail)
                {
                    try
                    {
                        var subject = "[EduChatbot] New Student Account Credentials";
                        var body = $@"Hello {fullName},

Your student account has been created on the EduChatbot system by the Administrator.

Your login credentials:
- Login Email: {email}
- Password: {randomPassword}

Please log in and change your password upon your first use.

Best regards,
EduChatbot System";

                        // IMPORTANT: Do not send email directly -> push to queue for background worker sending.
                        await _emailQueueService.EnqueueAsync(email, subject, body);
                        emailQueuedCount++;
                    }
                    catch
                    {
                        // Do not block import if email enqueue fails
                        emailQueueFailedCount++;
                    }
                }
                successCount++;
            }

            var msg = $"Successfully imported {successCount} student(s).";
            if (failedCount > 0)
            {
                msg += $" Failed {failedCount} row(s). Details: {string.Join("; ", errorMessages.Take(5))}";
                if (errorMessages.Count > 5) msg += "...";
            }

            if (sendEmail)
            {
                msg += $" Queued {emailQueuedCount} email(s).";
                if (emailQueueFailedCount > 0)
                {
                    msg += $" Failed to queue {emailQueueFailedCount} email(s).";
                }
            }

            return new AdminOperationResult { IsSuccess = successCount > 0, Message = msg };
        }
        catch (Exception ex)
        {
            return Failure($"Error processing Excel file: {ex.Message}");
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

    public async Task<AdminOperationResult> CreateCourseAsync(string code, string name, string description)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description))
        {
            return Failure("Course code, course name, and course description cannot be empty.");
        }

        var normalizedCode = code.Trim().ToUpperInvariant();
        var existing = await _context.Courses.AnyAsync(c => c.Code == normalizedCode);
        if (existing)
        {
            return Failure($"Course with code '{normalizedCode}' already exists.");
        }

        var course = new Course
        {
            Code = normalizedCode,
            Name = name.Trim(),
            Description = description.Trim()
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        return Success("Course created successfully.");
    }

    public async Task<AdminOperationResult> DeleteCourseAsync(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return Failure("Course not found.");
        }

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();

        return Success("Course deleted successfully.");
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
        if (lecturer == null) return Failure("Lecturer not found.");

        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return Failure("Course not found.");

        var isLecturer = await _userManager.IsInRoleAsync(lecturer, ApplicationRoles.Lecturer);
        if (!isLecturer) return Failure("User is not a lecturer.");

        var existing = await _context.LecturerCourses
            .AnyAsync(lc => lc.LecturerId == lecturerId && lc.CourseId == courseId);
        if (existing)
        {
            return Success("Lecturer is already assigned to this course.");
        }

        var assignment = new LecturerCourse
        {
            LecturerId = lecturerId,
            CourseId = courseId
        };

        _context.LecturerCourses.Add(assignment);
        await _context.SaveChangesAsync();

        return Success("Lecturer assigned to course successfully.");
    }

    public async Task<AdminOperationResult> RemoveLecturerFromCourseAsync(string lecturerId, int courseId)
    {
        var assignment = await _context.LecturerCourses
            .FirstOrDefaultAsync(lc => lc.LecturerId == lecturerId && lc.CourseId == courseId);
        if (assignment == null)
        {
            return Failure("Course assignment not found.");
        }

        _context.LecturerCourses.Remove(assignment);
        await _context.SaveChangesAsync();

        return Success("Lecturer course assignment removed successfully.");
    }

    public async Task<AdminOperationResult> ImportLecturersFromExcelAsync(Stream fileStream, bool sendEmail = true)
    {
        try
        {
            var rows = MiniExcel.Query(fileStream).ToList();
            if (rows.Count <= 1)
            {
                return Failure("The Excel file has no data or is empty.");
            }

            var header = rows[0] as IDictionary<string, object>;
            if (header == null) return Failure("Invalid Excel file format.");

            string emailKey = "";
            string nameKey = "";
            string courseCodesKey = "";

            foreach (var key in header.Keys)
            {
                var val = header[key]?.ToString()?.ToLowerInvariant() ?? "";
                if (val.Contains("email") || val.Contains("thư điện tử")) emailKey = key;
                else if (val.Contains("name") || val.Contains("tên") || val.Contains("họ tên")) nameKey = key;
                else if (val.Contains("course") || val.Contains("môn") || val.Contains("code")) courseCodesKey = key;
            }

            if (string.IsNullOrEmpty(emailKey) || string.IsNullOrEmpty(nameKey) || string.IsNullOrEmpty(courseCodesKey))
            {
                return Failure("Excel file requires 3 columns: FullName, Email, CourseCodes.");
            }

            int successCount = 0;
            int failedCount = 0;
            int emailQueuedCount = 0;
            int emailQueueFailedCount = 0;
            var errorMessages = new List<string>();

            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i] as IDictionary<string, object>;
                if (row == null) continue;

                var email = row[emailKey]?.ToString()?.Trim();
                var fullName = row[nameKey]?.ToString()?.Trim();
                var rawCourseCodes = row[courseCodesKey]?.ToString()?.Trim();

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName))
                {
                    failedCount++;
                    errorMessages.Add($"Row {i + 1}: Missing Email or FullName.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(rawCourseCodes))
                {
                    failedCount++;
                    errorMessages.Add($"Row {i + 1} ({email}): CourseCodes is empty.");
                    continue;
                }

                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    failedCount++;
                    errorMessages.Add($"Row {i + 1} ({email}): Email already exists.");
                    continue;
                }

                // Parse course codes list: split by comma, trim, uppercase.
                var courseCodes = rawCourseCodes
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(x => x.Trim().ToUpperInvariant())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

                if (courseCodes.Count == 0)
                {
                    failedCount++;
                    errorMessages.Add($"Row {i + 1} ({email}): Invalid CourseCodes.");
                    continue;
                }

                // Validate all course codes must exist.
                var courses = await _context.Courses
                    .Where(c => courseCodes.Contains(c.Code))
                    .ToListAsync();

                if (courses.Count != courseCodes.Count)
                {
                    var existingCodes = courses.Select(c => c.Code).ToHashSet();
                    var missing = courseCodes.Where(code => !existingCodes.Contains(code)).ToList();
                    failedCount++;
                    errorMessages.Add($"Row {i + 1} ({email}): CourseCode does not exist: {string.Join(", ", missing)}");
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

                var roleResult = await _userManager.AddToRoleAsync(user, ApplicationRoles.Lecturer);
                if (!roleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    failedCount++;
                    errorMessages.Add($"Row {i + 1} ({email}): Cannot assign Lecturer role.");
                    continue;
                }

                // Create LecturerCourse
                foreach (var course in courses)
                {
                    _context.LecturerCourses.Add(new LecturerCourse
                    {
                        LecturerId = user.Id,
                        CourseId = course.Id
                    });
                }
                await _context.SaveChangesAsync();

                if (sendEmail)
                {
                    try
                    {
                        var subject = "[EduChatbot] New Lecturer Account Credentials";
                        var assignedCourseNames = courses.Select(c => $"{c.Code} - {c.Name}").ToList();

                        var body = $@"Hello {fullName},

Your lecturer account has been created on the EduChatbot system by the Administrator.

Your login credentials:
- Login Email: {email}
- Password: {randomPassword}
- Assigned Courses: {string.Join(", ", assignedCourseNames)}

Please log in and change your password upon your first use.

Best regards,
EduChatbot System";

                        await _emailQueueService.EnqueueAsync(email, subject, body);
                        emailQueuedCount++;
                    }
                    catch
                    {
                        emailQueueFailedCount++;
                    }
                }

                successCount++;
            }

            var msg = $"Successfully imported {successCount} lecturer(s).";
            if (failedCount > 0)
            {
                msg += $" Failed {failedCount} row(s). Details: {string.Join("; ", errorMessages.Take(5))}";
                if (errorMessages.Count > 5) msg += "...";
            }

            if (sendEmail)
            {
                msg += $" Queued {emailQueuedCount} email(s).";
                if (emailQueueFailedCount > 0)
                {
                    msg += $" Failed to queue {emailQueueFailedCount} email(s).";
                }
            }

            return new AdminOperationResult { IsSuccess = successCount > 0, Message = msg };
        }
        catch (Exception ex)
        {
            return Failure($"Error processing Excel file: {ex.Message}");
        }
    }

    public async Task<AdminOperationResult> ImportCoursesFromExcelAsync(Stream fileStream)
    {
        try
        {
            var rows = MiniExcel.Query(fileStream).ToList();
            if (rows.Count <= 1)
            {
                return Failure("The Excel file has no data or is empty.");
            }

            var header = rows[0] as IDictionary<string, object>;
            if (header == null) return Failure("Invalid Excel file format.");

            string codeKey = "";
            string nameKey = "";
            string descKey = "";

            foreach (var key in header.Keys)
            {
                var val = header[key]?.ToString()?.ToLowerInvariant() ?? "";
                if (val.Contains("code") || val.Contains("mã") || val.Contains("mã môn")) codeKey = key;
                else if (val.Contains("name") || val.Contains("tên") || val.Contains("tên môn")) nameKey = key;
                else if (val.Contains("description") || val.Contains("mô tả")) descKey = key;
            }

            if (string.IsNullOrEmpty(codeKey) || string.IsNullOrEmpty(nameKey))
            {
                var keys = header.Keys.ToList();
                if (keys.Count >= 2)
                {
                    codeKey = keys[0];
                    nameKey = keys[1];
                    if (keys.Count >= 3)
                    {
                        descKey = keys[2];
                    }
                }
                else
                {
                    return Failure("Excel file requires at least 2 columns: Code and Name.");
                }
            }

            int successCount = 0;
            int failedCount = 0;
            var errorMessages = new List<string>();

            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i] as IDictionary<string, object>;
                if (row == null) continue;

                var code = row[codeKey]?.ToString()?.Trim();
                var name = row[nameKey]?.ToString()?.Trim();
                var description = !string.IsNullOrEmpty(descKey) ? row[descKey]?.ToString()?.Trim() : "";

                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
                {
                    failedCount++;
                    errorMessages.Add($"Row {i + 1}: Missing Course Code or Name.");
                    continue;
                }

                var normalizedCode = code.ToUpperInvariant();
                var existing = await _context.Courses.AnyAsync(c => c.Code == normalizedCode);
                if (existing)
                {
                    failedCount++;
                    errorMessages.Add($"Row {i + 1} ({code}): Course code already exists.");
                    continue;
                }

                var course = new Course
                {
                    Code = normalizedCode,
                    Name = name,
                    Description = description ?? ""
                };

                _context.Courses.Add(course);
                successCount++;
            }

            if (successCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            var msg = $"Successfully imported {successCount} course(s).";
            if (failedCount > 0)
            {
                msg += $" Failed {failedCount} row(s). Details: {string.Join("; ", errorMessages.Take(5))}";
                if (errorMessages.Count > 5) msg += "...";
            }

            return new AdminOperationResult { IsSuccess = successCount > 0, Message = msg };
        }
        catch (Exception ex)
        {
            return Failure($"Error processing Excel file: {ex.Message}");
        }
    }
}
