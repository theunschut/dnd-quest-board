
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using QuestBoard.Domain.Automapper;
using QuestBoard.Domain.Extensions;
using QuestBoard.Repository.Entities;
using QuestBoard.Repository.Extensions;
using QuestBoard.Service.Automapper;
using QuestBoard.Service.Authorization;

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

app.Services.ConfigureDatabase();

app.Run();