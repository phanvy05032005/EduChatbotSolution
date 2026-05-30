namespace EduChatbot.Business.Services;

public interface IAppStatusService
{
    DateTime AppStartTime { get; }
    string GetUptime();
}
