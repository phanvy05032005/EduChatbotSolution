namespace EduChatbot.Business.Services;

public class AppStatusService : IAppStatusService
{
    public DateTime AppStartTime { get; }

    public AppStatusService()
    {
        AppStartTime = DateTime.UtcNow;
    }

    public string GetUptime()
    {
        var uptime = DateTime.UtcNow - AppStartTime;
        return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
    }
}
