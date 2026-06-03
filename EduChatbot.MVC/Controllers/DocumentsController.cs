using EduChatbot.Business.Services;
using EduChatbot.MVC.Models;
using EduChatbot.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduChatbot.MVC.Controllers;

public class DocumentsController : Controller
{
    private readonly IDocumentService _documentService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly UserManager<ApplicationUser> _userManager;

    public DocumentsController(
        IDocumentService documentService,
        IWebHostEnvironment webHostEnvironment,
        UserManager<ApplicationUser> userManager)
    {
        _documentService = documentService;
        _webHostEnvironment = webHostEnvironment;
        _userManager = userManager;
    }

    [Authorize(Roles = ApplicationRoles.DocumentManagers)]
    public async Task<IActionResult> Upload()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var isAdmin = User.IsInRole(ApplicationRoles.Admin);
        var courses = await _documentService.GetAvailableCoursesForUserAsync(userId, isAdmin);
        ViewBag.Courses = courses;
        return View();
    }

    [HttpPost]
    [Authorize(Roles = ApplicationRoles.DocumentManagers)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile? documentFile, int courseId)
    {
        if (courseId <= 0)
        {
            ModelState.AddModelError("courseId", "Vui lòng chọn môn học.");
        }

        if (documentFile == null)
        {
            ModelState.AddModelError("documentFile", "Vui lòng chọn file PDF hoặc DOCX.");
        }

        if (!ModelState.IsValid)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var isAdmin = User.IsInRole(ApplicationRoles.Admin);
            ViewBag.Courses = await _documentService.GetAvailableCoursesForUserAsync(userId, isAdmin);
            return View();
        }

        await using var stream = documentFile!.OpenReadStream();
        var uploadedBy = await GetCurrentLecturerNameAsync();
        var result = await _documentService.UploadDocumentAsync(
            stream,
            documentFile.FileName,
            documentFile.ContentType,
            documentFile.Length,
            uploadedBy,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            _webHostEnvironment.WebRootPath,
            courseId);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError("documentFile", result.Message);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var isAdmin = User.IsInRole(ApplicationRoles.Admin);
            ViewBag.Courses = await _documentService.GetAvailableCoursesForUserAsync(userId, isAdmin);
            return View();
        }

        TempData["UploadMessage"] = result.Message;
        TempData["ChunkCount"] = result.ChunkCount;
        TempData["IndexStatus"] = result.Status;

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = ApplicationRoles.DocumentManagers)]
    public async Task<IActionResult> Index(string? searchTerm)
    {
        var result = await _documentService.GetDocumentsAsync(
            searchTerm,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.IsInRole(ApplicationRoles.Admin));
        return View(result);
    }

    [Authorize(Roles = ApplicationRoles.DocumentManagers)]
    public async Task<IActionResult> Details(int id)
    {
        var document = await _documentService.GetDocumentDetailsAsync(
            id,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.IsInRole(ApplicationRoles.Admin));
        if (document == null)
        {
            return NotFound();
        }

        return View(document);
    }

    [Authorize(Roles = ApplicationRoles.DocumentManagers)]
    public async Task<IActionResult> Edit(int id)
    {
        var document = await _documentService.GetDocumentDetailsAsync(
            id,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.IsInRole(ApplicationRoles.Admin));
        if (document == null)
        {
            return NotFound();
        }

        return View(new DocumentEditViewModel
        {
            Id = document.Id,
            FileName = document.FileName,
            StoredFileName = document.StoredFileName,
            FilePath = document.FilePath
        });
    }

    [HttpPost]
    [Authorize(Roles = ApplicationRoles.DocumentManagers)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(DocumentEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _documentService.UpdateDocumentAsync(
            model.Id,
            model.FileName,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.IsInRole(ApplicationRoles.Admin));

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["UploadMessage"] = result.Message;
        TempData["ChunkCount"] = result.ChunkCount;
        TempData["IndexStatus"] = result.Status;

        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [Authorize(Roles = ApplicationRoles.DocumentManagers)]
    public async Task<IActionResult> Dashboard()
    {
        var summary = await _documentService.GetDashboardSummaryAsync();
        return View(summary);
    }

    [HttpPost]
    [Authorize(Roles = ApplicationRoles.DocumentManagers)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _documentService.DeleteDocumentAsync(
            id,
            _webHostEnvironment.WebRootPath,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.IsInRole(ApplicationRoles.Admin));
        TempData[deleted ? "UploadMessage" : "ErrorMessage"] = deleted
            ? "Document deleted successfully."
            : "Document not found.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<string> GetCurrentLecturerNameAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (!string.IsNullOrWhiteSpace(user?.FullName))
        {
            return user.FullName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(user?.Email))
        {
            return user.Email.Trim();
        }

        return string.IsNullOrWhiteSpace(User.Identity?.Name)
            ? "Lecturer"
            : User.Identity.Name.Trim();
    }
}
