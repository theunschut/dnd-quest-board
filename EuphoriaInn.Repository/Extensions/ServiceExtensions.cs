using EuphoriaInn.Repository.Entities;
using EuphoriaInn.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EuphoriaInn.Repository.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddRepositoryServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Entity Framework
        services.AddDbContext<QuestBoardContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPlayerSignupRepository, PlayerSignupRepository>();
        services.AddScoped<IQuestRepository, QuestRepository>();
        services.AddScoped<IShopRepository, ShopRepository>();
        services.AddScoped<IUserTransactionRepository, UserTransactionRepository>();
        services.AddScoped<ITradeItemRepository, TradeItemRepository>();
        services.AddScoped<ICharacterRepository, CharacterRepository>();

        return services;
    }

    public static IServiceProvider ConfigureDatabase(this IServiceProvider services)
    {
        // Apply any pending migrations automatically
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<QuestBoardContext>();
        context.Database.Migrate();

        return services;
    }
}