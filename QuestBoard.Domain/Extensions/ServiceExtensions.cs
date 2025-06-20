using Microsoft.Extensions.DependencyInjection;
using QuestBoard.Domain.Interfaces;

namespace QuestBoard.Domain.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IDungeonMasterService, DungeonMasterService>();
        services.AddScoped<IPlayerSignupService, PlayerSignupService>();
        services.AddScoped<IQuestService, QuestService>();

        return services;
    }
}