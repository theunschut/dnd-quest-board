
using QuestBoard.Domain.Automapper;
using QuestBoard.Domain.Extensions;
using QuestBoard.Repository.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

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
    .AddDomainServices();

// Add automapper
builder.Services.AddAutoMapper(config =>
{
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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Services.ConfigureDatabase();

app.Run();