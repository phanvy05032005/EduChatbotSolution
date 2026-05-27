namespace EduChatbot.Models;

public class DocumentDashboardSummary
{
    public int TotalDocuments { get; set; }

    public int ReadyDocuments { get; set; }

    public int ProcessingDocuments { get; set; }

    public int FailedDocuments { get; set; }

    public int TotalChunks { get; set; }
}
