using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using EuphoriaInn.Repository.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.TestHost;

namespace EuphoriaInn.IntegrationTests.Helpers;

public static class AuthenticationHelper
{
    public static async Task<UserEntity> CreateTestUserAsync(
        IServiceProvider services,
        string userName = "testuser",
        string email = "test@example.com",
        string password = "Test123!",
        string name = "Test User")
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserEntity>>();

        // Make usernames and emails unique to avoid conflicts across tests
        var uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);
        var uniqueUserName = $"{userName}_{uniqueSuffix}";
        var uniqueEmail = email.Replace("@", $"_{uniqueSuffix}@");

        var user = new UserEntity
        {
            UserName = uniqueUserName,
            Email = uniqueEmail,
            EmailConfirmed = true,
            Name = name,
            HasKey = false
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new Exception($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return user;
    }

    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        WebApplicationFactory<Program> factory,
        string userName = "testuser",
        string email = "test@example.com",
        string password = "Test123!",
        string name = "Test User",
        string[]? roles = null)
    {
        var user = await CreateTestUserAsync(factory.Services, userName, email, password, name);

        // Get user roles from database if not specified
        if (roles == null)
        {
            using var scope = factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserEntity>>();
            var userFromDb = await userManager.FindByIdAsync(user.Id.ToString());
            if (userFromDb != null)
            {
                roles = (await userManager.GetRolesAsync(userFromDb)).ToArray();
            }
            roles ??= new[] { "Player" }; // Default to Player role
        }

        // Create client - Test authentication scheme is now registered globally in WebApplicationFactoryBase
        var client = factory.CreateClient();

        // Encode user info with roles in the authorization header
        var userInfo = $"{user.Id}:{user.UserName}:{user.Email}:{string.Join(",", roles)}";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", userInfo);

        return client;
    }

    public static async Task<(HttpClient client, UserEntity user)> CreateAuthenticatedClientWithUserAsync(
        WebApplicationFactory<Program> factory,
        string userName = "testuser",
        string email = "test@example.com",
        string password = "Test123!",
        string name = "Test User",
        string[]? roles = null)
    {
        var user = await CreateTestUserAsync(factory.Services, userName, email, password, name);

        // Add roles to the user in the database if specified
        if (roles != null && roles.Length > 0)
        {
            using var scope = factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserEntity>>();
            var userFromDb = await userManager.FindByIdAsync(user.Id.ToString());
            if (userFromDb != null)
            {
                foreach (var role in roles)
                {
                    // Check if role exists, create if not
                    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole<int>(role));
                    }
                    await userManager.AddToRoleAsync(userFromDb, role);
                }
            }
        }

        var client = await CreateAuthenticatedClientAsync(factory, userName, email, password, name, roles);
        return (client, user);
    }

    public static async Task<(HttpClient client, UserEntity user)> CreateAuthenticatedAdminClientAsync(
        WebApplicationFactory<Program> factory,
        string userName = "adminuser",
        string email = "admin@example.com",
        string password = "Admin123!",
        string name = "Admin User")
    {
        return await CreateAuthenticatedClientWithUserAsync(factory, userName, email, password, name, new[] { "Admin" });
    }

    public static async Task<(HttpClient client, UserEntity user)> CreateAuthenticatedDMClientAsync(
        WebApplicationFactory<Program> factory,
        string userName = "dmuser",
        string email = "dm@example.com",
        string password = "DMpass123!",
        string name = "DM User")
    {
        return await CreateAuthenticatedClientWithUserAsync(factory, userName, email, password, name, new[] { "DungeonMaster" });
    }
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Test "))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userInfo = authHeader.Substring("Test ".Length);
        var parts = userInfo.Split(':');

        if (parts.Length < 4)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid auth header format"));
        }

        var userId = parts[0];
        var userName = parts[1];
        var email = parts[2];
        var rolesStr = parts[3];

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Email, email)
        };

        // Add role claims
        if (!string.IsNullOrEmpty(rolesStr))
        {
            var roles = rolesStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
