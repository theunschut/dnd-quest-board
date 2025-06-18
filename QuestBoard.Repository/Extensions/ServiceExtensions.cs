using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestBoard.Repository.Entities;

namespace QuestBoard.Repository.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddRepositoryServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Entity Framework
        services.AddDbContext<QuestBoardContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IQuestRepository, QuestRepository>();

        return services;
    }

    public static IServiceProvider ConfigureDatabase(this IServiceProvider services)
    {
        // Ensure database is created
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<QuestBoardContext>();
        context.Database.EnsureCreated();

        return services;
    }
}