using EduChatbot.Business.Services;
using EduChatbot.MVC.Models;
using EduChatbot.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IO;
using System.Reflection;
using MiniExcelLibs;

namespace EduChatbot.MVC.Controllers;

[Authorize(Roles = ApplicationRoles.Admin)]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly IDocumentService _documentService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public AdminController(
        IAdminService adminService,
        IDocumentService documentService,
        IWebHostEnvironment webHostEnvironment)
    {
        _adminService = adminService;
        _documentService = documentService;
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

    public async Task<IActionResult> CreateLecturer()
    {
        var courses = await _adminService.GetCoursesAsync();
        return View("AccountForm", new AdminAccountFormViewModel
        {
            AccountType = ApplicationRoles.Lecturer,
            AvailableCourses = courses
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
            if (model.AccountType == ApplicationRoles.Lecturer)
            {
                model.AvailableCourses = await _adminService.GetCoursesAsync();
            }
            return View("AccountForm", model);
        }

        var result = await _adminService.CreateAccountAsync(
            model.FullName,
            model.Email,
            model.Password!,
            model.AccountType,
            // Tài khoản do Admin tạo phải tự động gửi thông tin đăng nhập cho người dùng.
            true,
            model.SelectedCourseIds);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            if (model.AccountType == ApplicationRoles.Lecturer)
            {
                model.AvailableCourses = await _adminService.GetCoursesAsync();
            }
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
    public async Task<IActionResult> ImportStudents(IFormFile? excelFile)
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
        // Import sinh viên cũng tự động gửi email để sinh viên nhận thông tin đăng nhập ngay.
        var result = await _adminService.ImportStudentsFromExcelAsync(stream, true);

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
    public async Task<IActionResult> CreateCourse(string code, string name, string description)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description))
        {
            TempData["AdminError"] = "Mã môn học, tên môn học và mô tả môn học không được để trống.";
            return RedirectToAction(nameof(Courses));
        }

        var result = await _adminService.CreateCourseAsync(code, name, description);
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

    public async Task<IActionResult> PendingDocuments()
    {
        var documents = await _documentService.GetPendingReviewDocumentsAsync();
        return View(documents);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveDocument(int id)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _documentService.ApproveDocumentAsync(id, adminId);
        TempData[result.IsSuccess ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(PendingDocuments));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectDocument(int id, string? reviewNote)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _documentService.RejectDocumentAsync(id, adminId, reviewNote);
        TempData[result.IsSuccess ? "AdminMessage" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(PendingDocuments));
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportLecturers(IFormFile? excelFile)
    {
        if (excelFile == null || excelFile.Length == 0)
        {
            TempData["AdminError"] = "Vui lòng chọn file Excel.";
            return RedirectToAction(nameof(Lecturers));
        }

        if (!Path.GetExtension(excelFile.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            TempData["AdminError"] = "Hệ thống chỉ hỗ trợ file định dạng .xlsx.";
            return RedirectToAction(nameof(Lecturers));
        }

        await using var stream = excelFile.OpenReadStream();
        var result = await _adminService.ImportLecturersFromExcelAsync(stream, true);
        if (result.IsSuccess)
        {
            TempData["AdminMessage"] = result.Message;
        }
        else
        {
            TempData["AdminError"] = result.Message;
        }

        return RedirectToAction(nameof(Lecturers));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportCourses(IFormFile? excelFile)
    {
        if (excelFile == null || excelFile.Length == 0)
        {
            TempData["AdminError"] = "Vui lòng chọn file Excel.";
            return RedirectToAction(nameof(Courses));
        }

        if (!Path.GetExtension(excelFile.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            TempData["AdminError"] = "Hệ thống chỉ hỗ trợ file định dạng .xlsx.";
            return RedirectToAction(nameof(Courses));
        }

        await using var stream = excelFile.OpenReadStream();
        var result = await _adminService.ImportCoursesFromExcelAsync(stream);

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

    [HttpGet]
    public IActionResult DownloadCourseTemplate()
    {
        var memoryStream = new MemoryStream();
        var values = new[]
        {
            new { Code = "PRN222", Name = "C# & .NET Cloud", Description = "ASP.NET Core, MVC, Entity Framework Core" },
            new { Code = "SWR302", Name = "Software Requirement", Description = "Software requirements elicitation, analysis, and validation" }
        };
        MiniExcel.SaveAs(memoryStream, values);
        memoryStream.Seek(0, SeekOrigin.Begin);
        return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CourseImportTemplate.xlsx");
    }
}
