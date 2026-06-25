using EuphoriaInn.Repository.Automapper;
using EuphoriaInn.Domain.Extensions;
using EuphoriaInn.Repository.Entities;
using EuphoriaInn.Repository.Extensions;
using EuphoriaInn.Service.Authorization;
using EuphoriaInn.Service.Automapper;
using EuphoriaInn.Service.Middleware;
using EuphoriaInn.Service.ViewExpanders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.SqlServer;
using EuphoriaInn.Service.Jobs;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel server limits
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB (slightly higher than validation to allow for form overhead)
});


// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationExpanders.Add(new MobileViewLocationExpander());
});

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

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            }));

    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = 2;
    });
}

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
app.UseMiddleware<MobileDetectionMiddleware>();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/hangfire"))
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                context.Response.Redirect("/Account/Login");
                return;
            }

            if (!context.User.IsInRole("Admin"))
            {
                context.Response.Redirect("/Account/Login");
                return;
            }
        }

        await next();
    });

    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new AdminDashboardAuthFilter() }
    });
}

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

    // Smoke-test: proves IServiceScopeFactory pattern resolves before real jobs land
    // REMOVE THIS in Phase 21 once real jobs exist
    BackgroundJob.Enqueue<SmokeTestJob>(j => j.RunAsync(CancellationToken.None));
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