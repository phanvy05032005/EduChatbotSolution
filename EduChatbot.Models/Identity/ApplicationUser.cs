using Microsoft.AspNetCore.Identity;

namespace EduChatbot.Models.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
