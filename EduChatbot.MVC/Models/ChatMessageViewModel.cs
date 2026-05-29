namespace EduChatbot.MVC.Models;

public class ChatMessageViewModel
{
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public List<ChatSourceViewModel> Sources { get; set; } = [];

    public string CreatedAt { get; set; } = string.Empty;
}

public class ChatSourceViewModel
{
    public string Doc { get; set; } = string.Empty;

    public int Chunk { get; set; }
}
