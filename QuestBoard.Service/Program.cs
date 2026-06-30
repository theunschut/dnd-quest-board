using QuestBoard.Repository.Automapper;
using QuestBoard.Service.Extensions;
using QuestBoard.Domain.Extensions;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Repository.Entities;
using QuestBoard.Repository.Extensions;
using QuestBoard.Service.Authorization;
using QuestBoard.Service.Automapper;
using QuestBoard.Service.Middleware;
using QuestBoard.Service.Services;
using QuestBoard.Service.ViewExpanders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.SqlServer;
using QuestBoard.Service.Jobs;

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
        policy.Requirements.Add(new AdminRequirement()))
    .AddPolicy("SuperAdminOnly", policy =>
        policy.RequireRole("SuperAdmin"));

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

// Named HttpClient for Resend API stats (D-10)
// Authorization header is NOT set here — added per-request in AdminController.GetResendStatsAsync (Pitfall 4)
builder.Services.AddHttpClient("Resend", client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Email render service and job dispatcher (Service-layer registrations)
builder.Services.AddScoped<IEmailRenderService, RazorEmailRenderService>();

// IActiveGroupContext — registered as Scoped; same scope as QuestBoardContext.
// In Testing environment the WebApplicationFactoryBase overrides this with MutableGroupContext singleton.
builder.Services.AddHttpContextAccessor();

// D-09 dual registration pattern (see STATE.md):
//   1. AddScoped<ActiveGroupContextService>() — registers the CONCRETE type so that Hangfire
//      jobs (QuestFinalizedEmailJob, SessionReminderJob) can resolve it by concrete type and
//      call SetGroupId(groupId), which is NOT on the IActiveGroupContext interface.
//   2. AddScoped<IActiveGroupContext>(factory) — satisfies constructor-injected IActiveGroupContext
//      in controllers and domain services; the factory delegates to the SAME scoped instance,
//      so SetGroupId mutations are immediately visible to QuestBoardContext within the same scope.
// IMPORTANT: both registrations must stay in sync. Do NOT replace with AddScoped<IActiveGroupContext,
// ActiveGroupContextService>() alone — that breaks concrete-type resolution in the Hangfire jobs.
builder.Services.AddScoped<ActiveGroupContextService>();
builder.Services.AddScoped<IActiveGroupContext>(sp =>
    sp.GetRequiredService<ActiveGroupContextService>());

if (!builder.Environment.IsEnvironment("Testing"))
{
    // HangfireQuestEmailDispatcher requires IBackgroundJobClient which is only
    // registered when Hangfire is active (non-Testing environments).
    builder.Services.AddScoped<IQuestEmailDispatcher, HangfireQuestEmailDispatcher>();
    builder.Services.AddScoped<IReminderJobDispatcher, HangfireReminderJobDispatcher>();

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
else
{
    // In the Testing environment Hangfire is skipped, so use a no-op dispatcher.
    builder.Services.AddScoped<IQuestEmailDispatcher, NullQuestEmailDispatcher>();
    builder.Services.AddScoped<IReminderJobDispatcher, NullReminderJobDispatcher>();
}

// Add automapper
builder.Services.AddAutoMapper(config =>
{
    config.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    config.AddProfile<ViewModelProfile>();
    config.AddProfile<EntityProfile>();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.Configuration.DumpConfiguration();

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

    // Register daily session reminder sweep — runs at 09:00 server local time (CET/CEST).
    // Placed after ConfigureDatabase to ensure migrations have run before the job can fire (RESEARCH.md Pitfall 4).
    RecurringJob.AddOrUpdate<DailyReminderJob>(
        "daily-session-reminders",
        job => job.ExecuteAsync(CancellationToken.None),
        "0 9 * * *");
}

app.Run();

static async Task SeedShopDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    try
    {
        var shopSeedService = scope.ServiceProvider.GetRequiredService<QuestBoard.Domain.Interfaces.IShopSeedService>();
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