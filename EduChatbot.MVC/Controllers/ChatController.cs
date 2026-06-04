using System.Security.Claims;
using System.Text.Json;
using EduChatbot.Business.Services;
using EduChatbot.MVC.Models;
using EduChatbot.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduChatbot.MVC.Controllers;

[Authorize(Roles = ApplicationRoles.Student)]
public class ChatController : Controller
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var conversations = await _chatService.GetConversationsAsync(userId);
        ViewBag.Courses = await _chatService.GetCoursesAsync();
        return View(conversations);
    }

    public async Task<IActionResult> Conversation(int? id, int? courseId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var conversation = await _chatService.GetOrCreateConversationAsync(id, userId, courseId);
        ViewBag.Conversations = await _chatService.GetConversationsAsync(userId);
        ViewBag.Courses = await _chatService.GetCoursesAsync();
        return View(conversation);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(int conversationId, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return BadRequest(new { error = "Vui lòng nhập câu hỏi." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var aiMessage = await _chatService.SendMessageAsync(conversationId, userId, message.Trim());

        // Parse source chunks từ JSON string.
        var sources = new List<ChatSourceViewModel>();
        if (!string.IsNullOrWhiteSpace(aiMessage.SourceChunks))
        {
            try
            {
                using var doc = JsonDocument.Parse(aiMessage.SourceChunks);
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    sources.Add(new ChatSourceViewModel
                    {
                        Doc = element.GetProperty("doc").GetString() ?? "",
                        Chunk = element.GetProperty("chunk").GetInt32()
                    });
                }
            }
            catch
            {
                // Nếu parse lỗi thì bỏ qua sources.
            }
        }

        var viewModel = new ChatMessageViewModel
        {
            Role = aiMessage.Role,
            Content = aiMessage.Content,
            Sources = sources,
            CreatedAt = aiMessage.CreatedAt.ToString("HH:mm dd/MM/yyyy")
        };

        return Json(viewModel);
    }
}
