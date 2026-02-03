
using EuphoriaInn.Domain.Automapper;
using EuphoriaInn.Domain.Extensions;
using EuphoriaInn.Repository.Entities;
using EuphoriaInn.Repository.Extensions;
using EuphoriaInn.Service.Authorization;
using EuphoriaInn.Service.Automapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add health checks
builder.Services.AddHealthChecks();

// Add Identity using existing QuestBoardContext
builder.Services.AddIdentity<UserEntity, IdentityRole<int>>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<QuestBoardContext>()
.AddDefaultTokenProviders();

// Add Authorization policies
builder.Services.AddScoped<IAuthorizationHandler, DungeonMasterHandler>();
builder.Services.AddScoped<IAuthorizationHandler, AdminHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DungeonMasterOnly", policy =>
        policy.Requirements.Add(new DungeonMasterRequirement()));
    options.AddPolicy("AdminOnly", policy =>
        policy.Requirements.Add(new AdminRequirement()));
});

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add repositories
builder.Services
    .AddRepositoryServices(builder.Configuration)
    .AddDomainServices(builder.Configuration);

// Add automapper
builder.Services.AddAutoMapper(config =>
{
    config.AddProfile<ViewModelProfile>();
    config.AddProfile<EntityProfile>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHealthChecks("/health");

// Only run migrations if not in testing environment
if (!app.Environment.IsEnvironment("Testing"))
{
    app.Services.ConfigureDatabase();

    // Seed basic shop data
    await SeedShopDataAsync(app);
}

app.Run();

static async Task SeedShopDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    try
    {
        var shopSeedService = scope.ServiceProvider.GetRequiredService<EuphoriaInn.Domain.Interfaces.IShopSeedService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserEntity>>();

        // Find first admin/DM user to attribute seed data to
        var adminUser = await userManager.Users.FirstOrDefaultAsync();
        if (adminUser != null)
        {
            await shopSeedService.SeedBasicEquipmentAsync(adminUser.Id);
        }
    }
    catch (Exception ex)
    {
        // Log error but don't stop application startup
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error seeding shop data");
    }
}

// Make Program class accessible to integration tests
public partial class Program { }