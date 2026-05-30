using EduChatbot.Business.Services;
using EduChatbot.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduChatbot.MVC.Controllers;

public class DocumentsController : Controller
{
    private readonly IDocumentService _documentService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public DocumentsController(
        IDocumentService documentService,
        IWebHostEnvironment webHostEnvironment)
    {
        _documentService = documentService;
        _webHostEnvironment = webHostEnvironment;
    }

    [Authorize(Roles = ApplicationRoles.DocumentManagers)]
    public IActionResult Upload()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Roles = ApplicationRoles.DocumentManagers)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile? documentFile, string uploadedBy = "Lecturer")
    {
        if (documentFile == null)
        {
            ModelState.AddModelError("documentFile", "Vui lòng chọn file PDF hoặc DOCX.");
            return View();
        }

        // Controller chỉ nhận request và gọi Service, không xử lý DB trực tiếp.
        await using var stream = documentFile.OpenReadStream();
        var result = await _documentService.UploadDocumentAsync(
            stream,
            documentFile.FileName,
            documentFile.ContentType,
            documentFile.Length,
            uploadedBy,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            _webHostEnvironment.WebRootPath);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError("documentFile", result.Message);
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

    [HttpGet]
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

        return View(document);
    }

    [HttpPost]
    [Authorize(Roles = ApplicationRoles.DocumentManagers)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            ModelState.AddModelError("fileName", "Tên tài liệu không được để trống.");
            var document = await _documentService.GetDocumentDetailsAsync(
                id,
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                User.IsInRole(ApplicationRoles.Admin));
            return View(document);
        }

        var updated = await _documentService.UpdateDocumentNameAsync(
            id,
            fileName,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.IsInRole(ApplicationRoles.Admin));

        TempData[updated ? "UploadMessage" : "ErrorMessage"] = updated
            ? "Đã cập nhật tên tài liệu thành công."
            : "Không thể cập nhật tài liệu.";

        return RedirectToAction(nameof(Index));
    }
}
