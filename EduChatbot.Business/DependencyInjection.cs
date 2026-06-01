using EduChatbot.Business.Services;
using EduChatbot.Data;
using EduChatbot.Data.Identity;
using EduChatbot.Data.Repositories;
using EduChatbot.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pgvector.EntityFrameworkCore;

namespace EduChatbot.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddEduChatbotApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.UseVector()));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
        });

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentService, DocumentService>();

        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IChatService, ChatService>();
        services.Configure<OpenRouterSettings>(configuration.GetSection("OpenRouter"));
        services.Configure<EmbeddingSettings>(configuration.GetSection("Embedding"));
        services.AddHttpClient<IEmbeddingService, OpenRouterEmbeddingService>();
        services.AddHttpClient<IChatService, ChatService>();

        return services;
    }

    public static Task SeedEduChatbotIdentityAsync(this IServiceProvider serviceProvider)
    {
        return IdentitySeeder.SeedAsync(serviceProvider);
    }
}
