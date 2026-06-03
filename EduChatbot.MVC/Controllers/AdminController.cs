using EduChatbot.Business.Services;
using EduChatbot.MVC.Models;
using EduChatbot.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Reflection;

namespace EduChatbot.MVC.Controllers;

[Authorize(Roles = ApplicationRoles.Admin)]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public AdminController(
        IAdminService adminService,
        IWebHostEnvironment webHostEnvironment)
    {
        _adminService = adminService;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<IActionResult> Dashboard()
    {
        var statistics = await _adminService.GetStatisticsAsync();
        var model = new AdminDashboardViewModel
        {
            TotalStudents = statistics.TotalStudents,
            TotalLecturers = statistics.TotalLecturers,
            TotalDocuments = statistics.TotalDocuments,
            TotalChatQuestions = statistics.TotalQuestionsAsked,
            RecentActivities =
            [
                "Student account created",
                "Lecturer account updated",
                "User role changed",
                "System settings updated"
            ]
        };

        return View(model);
    }

    public IActionResult Index()
    {
        return RedirectToAction(nameof(Dashboard));
    }

    public IActionResult Users()
    {
        return RedirectToAction(nameof(Dashboard));
    }

    public async Task<IActionResult> Students(string? searchTerm)
    {
        return View("Accounts", await BuildAccountListAsync(
            ApplicationRoles.Student,
            "Student Accounts",
            searchTerm));
    }

    public async Task<IActionResult> Lecturers(string? searchTerm)
    {
        return View("Accounts", await BuildAccountListAsync(
            ApplicationRoles.Lecturer,
            "Lecturer Accounts",
            searchTerm));
    }

    public IActionResult CreateStudent()
    {
        return View("AccountForm", new AdminAccountFormViewModel
        {
            AccountType = ApplicationRoles.Student
        });
    }

    public IActionResult CreateLecturer()
    {
        return View("AccountForm", new AdminAccountFormViewModel
        {
            AccountType = ApplicationRoles.Lecturer
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAccount(AdminAccountFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Please enter the password.");
        }

        if (!ModelState.IsValid)
        {
            return View("AccountForm", model);
        }

        var result = await _adminService.CreateAccountAsync(
            model.FullName,
            model.Email,
            model.Password!,
            model.AccountType,
            model.SendEmail);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View("AccountForm", model);
        }

        TempData["AdminMessage"] = result.Message;
        return RedirectToAccountList(model.AccountType);
    }

    public async Task<IActionResult> EditAccount(string id)
    {
        var account = await _adminService.GetAccountForEditAsync(id);
        if (account == null)
        {
            return NotFound();
        }

        return View("AccountForm", new AdminAccountFormViewModel
        {
            Id = account.Id,
            AccountType = account.Role,
            FullName = account.FullName,
            Email = account.Email
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAccount(AdminAccountFormViewModel model)
    {
        ModelState.Remove(nameof(model.Password));
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Id))
        {
            return View("AccountForm", model);
        }

        var result = await _adminService.UpdateAccountAsync(
            model.Id,
            model.FullName,
            model.Email);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View("AccountForm", model);
        }

        TempData["AdminMessage"] = result.Message;
        return RedirectToAccountList(model.AccountType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LockAccount(string id, string accountType)
    {
        var result = await _adminService.LockAccountAsync(id);
        TempData[result.IsSuccess ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAccountList(accountType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlockAccount(string id, string accountType)
    {
        var result = await _adminService.UnlockAccountAsync(id);
        TempData[result.IsSuccess ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAccountList(accountType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(string id, string accountType)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _adminService.DeleteAccountAsync(id, currentUserId);
        TempData[result.IsSuccess ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAccountList(accountType);
    }

    public IActionResult Roles()
    {
        var roles = new List<AdminRolePermissionViewModel>
        {
            new()
            {
                RoleName = ApplicationRoles.Student,
                Permissions = ["Login", "Ask Chatbot", "View Own Chat History"]
            },
            new()
            {
                RoleName = ApplicationRoles.Lecturer,
                Permissions = ["Login", "Upload Documents", "Manage Own Documents", "Run Evaluation"]
            },
            new()
            {
                RoleName = ApplicationRoles.Admin,
                Permissions = ["Manage Accounts", "Manage Roles", "Manage System", "View Statistics"]
            }
        };

        return View(roles);
    }

    public async Task<IActionResult> System()
    {
        var model = new AdminSystemStatusViewModel
        {
            DatabaseStatus = await _adminService.CanConnectToDatabaseAsync() ? "Connected" : "Unavailable",
            ApplicationStatus = "Running",
            StorageUsage = FormatBytes(GetUploadStorageUsage()),
            SystemVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0"
        };

        return View(model);
    }

    public async Task<IActionResult> Statistics()
    {
        var statistics = await _adminService.GetStatisticsAsync();
        var model = new AdminStatisticsViewModel
        {
            TotalStudents = statistics.TotalStudents,
            TotalLecturers = statistics.TotalLecturers,
            TotalDocuments = statistics.TotalDocuments,
            TotalQuestionsAsked = statistics.TotalQuestionsAsked
        };

        return View(model);
    }

    private async Task<AdminAccountListViewModel> BuildAccountListAsync(string role, string title, string? searchTerm)
    {
        var accounts = await _adminService.GetAccountsByRoleAsync(role, searchTerm);

        return new AdminAccountListViewModel
        {
            Title = title,
            AccountType = role,
            SearchTerm = searchTerm?.Trim() ?? string.Empty,
            Accounts = accounts
                .Select(account => new AdminAccountRowViewModel
                {
                    Id = account.Id,
                    FullName = account.FullName,
                    Email = account.Email,
                    Department = account.Department,
                    Status = account.Status
                })
                .ToList()
        };
    }

    private long GetUploadStorageUsage()
    {
        var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
        if (!Directory.Exists(uploadPath))
        {
            return 0;
        }

        return Directory
            .EnumerateFiles(uploadPath, "*", SearchOption.AllDirectories)
            .Sum(file => new FileInfo(file).Length);
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
        {
            return "0 MB";
        }

        return $"{bytes / 1024d / 1024d:0.00} MB";
    }

    private IActionResult RedirectToAccountList(string role)
    {
        return role == ApplicationRoles.Lecturer
            ? RedirectToAction(nameof(Lecturers))
            : RedirectToAction(nameof(Students));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportStudents(IFormFile? excelFile, bool sendEmail = false)
    {
        if (excelFile == null || excelFile.Length == 0)
        {
            TempData["AdminError"] = "Vui lòng chọn file Excel.";
            return RedirectToAction(nameof(Students));
        }

        if (!Path.GetExtension(excelFile.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            TempData["AdminError"] = "Hệ thống chỉ hỗ trợ file định dạng .xlsx.";
            return RedirectToAction(nameof(Students));
        }

        await using var stream = excelFile.OpenReadStream();
        var result = await _adminService.ImportStudentsFromExcelAsync(stream, sendEmail);

        if (result.IsSuccess)
        {
            TempData["AdminMessage"] = result.Message;
        }
        else
        {
            TempData["AdminError"] = result.Message;
        }

        return RedirectToAction(nameof(Students));
    }

    public async Task<IActionResult> Courses()
    {
        var courses = await _adminService.GetCoursesAsync();
        var lecturers = await _adminService.GetLecturersAsync();

        var model = new AdminCoursesViewModel
        {
            Courses = courses,
            Lecturers = lecturers
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCourse(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            TempData["AdminError"] = "Mã môn học và tên môn học không được để trống.";
            return RedirectToAction(nameof(Courses));
        }

        var result = await _adminService.CreateCourseAsync(code, name);
        if (result.IsSuccess)
        {
            TempData["AdminMessage"] = result.Message;
        }
        else
        {
            TempData["AdminError"] = result.Message;
        }

        return RedirectToAction(nameof(Courses));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var result = await _adminService.DeleteCourseAsync(id);
        if (result.IsSuccess)
        {
            TempData["AdminMessage"] = result.Message;
        }
        else
        {
            TempData["AdminError"] = result.Message;
        }

        return RedirectToAction(nameof(Courses));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignLecturer(string lecturerId, int courseId)
    {
        var result = await _adminService.AssignLecturerToCourseAsync(lecturerId, courseId);
        if (result.IsSuccess)
        {
            TempData["AdminMessage"] = result.Message;
        }
        else
        {
            TempData["AdminError"] = result.Message;
        }

        return RedirectToAction(nameof(Courses));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveLecturer(string lecturerId, int courseId)
    {
        var result = await _adminService.RemoveLecturerFromCourseAsync(lecturerId, courseId);
        if (result.IsSuccess)
        {
            TempData["AdminMessage"] = result.Message;
        }
        else
        {
            TempData["AdminError"] = result.Message;
        }

        return RedirectToAction(nameof(Courses));
    }
}
