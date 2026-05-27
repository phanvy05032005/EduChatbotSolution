using Microsoft.AspNetCore.Identity;

namespace EduChatbot.Data.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
