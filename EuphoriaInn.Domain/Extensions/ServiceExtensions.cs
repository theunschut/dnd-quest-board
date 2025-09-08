using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EuphoriaInn.Domain.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IPlayerSignupService, PlayerSignupService>();
        services.AddScoped<IQuestService, QuestService>();
        services.AddScoped<IShopService, ShopService>();

        return services;
    }
}