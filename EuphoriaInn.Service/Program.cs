using EuphoriaInn.Repository.Automapper;
using EuphoriaInn.Domain.Extensions;
using EuphoriaInn.Repository.Entities;
using EuphoriaInn.Repository.Extensions;
using EuphoriaInn.Service.Authorization;
using EuphoriaInn.Service.Automapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel server limits
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB (slightly higher than validation to allow for form overhead)
});

// Configure IIS server limits (if running on IIS)
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
});

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
    options.Password.RequiredLength = 8;

    // User settings
    options.User.RequireUniqueEmail = true;

    // Lockout settings
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<QuestBoardContext>()
.AddDefaultTokenProviders();

// Add Authorization policies
builder.Services.AddScoped<IAuthorizationHandler, DungeonMasterHandler>();
builder.Services.AddScoped<IAuthorizationHandler, AdminHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("DungeonMasterOnly", policy =>
        policy.Requirements.Add(new DungeonMasterRequirement()))
    .AddPolicy("AdminOnly", policy =>
        policy.Requirements.Add(new AdminRequirement()));

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
    config.LicenseKey = "eyJhbGciOiJSUzI1NiIsImtpZCI6Ikx1Y2t5UGVubnlTb2Z0d2FyZUxpY2Vuc2VLZXkvYmJiMTNhY2I1OTkwNGQ4OWI0Y2IxYzg1ZjA4OGNjZjkiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL2x1Y2t5cGVubnlzb2Z0d2FyZS5jb20iLCJhdWQiOiJMdWNreVBlbm55U29mdHdhcmUiLCJleHAiOiIxODA4MjY1NjAwIiwiaWF0IjoiMTc3NjgwNzM1MyIsImFjY291bnRfaWQiOiIwMTlkYjFmODQzMGE3ZTRhYWMzZmU1N2Q5M2ZjMzY3OCIsImN1c3RvbWVyX2lkIjoiY3RtXzAxa3Byemg3ZmdhZnRhOHZkOTU3NXg4dmpqIiwic3ViX2lkIjoiLSIsImVkaXRpb24iOiIwIiwidHlwZSI6IjIifQ.eW0lnu0panAyi5lyjRYbHP1a2q9VDo-QJDrJQqzgXgIcl6lrOzk2Yld7XI_sTyrr-lPtCp8KmsHI5kUMtk_ZEpTxEHyl5rvpDia9cJ9Pj-KPW-hFQU-XphEzNtnbzelCkX9UBTmmZSK9ZYpeQrlfjlbApocIFl-rGuKgTzyJEGlLDN_zo4xNVk_WcMA-YrFL2xOFJ4xtbkXYEZu25LjBg4hYaLGvGoS6sWm0258eU_m1Sd5UAkpkUaoQju6L6yq1G4hCQHFNv6395oezBzC9JV8WCTc6tEXp4GzRgLyBBmI_ZHFblNMEcR_k8xaWqd2LMVXjESSN3SOhhe6_VJCkjw";
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