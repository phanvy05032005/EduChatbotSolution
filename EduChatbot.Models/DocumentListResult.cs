namespace EduChatbot.Models;

public class DocumentListResult
{
    public List<Document> Documents { get; set; } = [];

    public string SearchTerm { get; set; } = string.Empty;

    public int TotalCount { get; set; }
}
