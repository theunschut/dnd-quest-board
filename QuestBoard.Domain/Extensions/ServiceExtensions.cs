using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestBoard.Domain.Configuration;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Services;
using QuestBoard.Domain.Services.Users;

namespace QuestBoard.Domain.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IPlayerService, PlayerService>();
        services.AddScoped<IPlayerSignupService, PlayerSignupService>();
        services.AddScoped<IQuestService, QuestService>();

        services.AddScoped<IPasswordHashingService, PasswordHashingService>();

        // Optional: Configure PBKDF2 parameters from appsettings.json
        services.AddScoped<IPasswordHashingService>(provider =>
        {
            var security = configuration.GetSection("Security").Get<SecurityConfiguration>();
            var iterations = security?.PasswordIterations ?? 100000;
            var saltSize = security?.SaltSize ?? 32;
            var hashSize = security?.HashSize ?? 32;
            return new PasswordHashingService(saltSize, hashSize, iterations);
        });

        return services;
    }
}