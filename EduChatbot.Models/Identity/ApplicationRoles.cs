namespace EduChatbot.Models.Identity;

public static class ApplicationRoles
{
    public const string Student = "Student";
    public const string Lecturer = "Lecturer";
    public const string Admin = "Admin";
    public const string DocumentManagers = Lecturer + "," + Admin;
}
