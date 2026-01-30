using EuphoriaInn.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;

namespace EuphoriaInn.IntegrationTests;

public class WebApplicationFactoryBase : WebApplicationFactory<Program>
{
    public TestDatabase Database { get; }

    public WebApplicationFactoryBase()
    {
        Database = new TestDatabase($"QuestBoardTest_{Guid.NewGuid():N}");
    }

    protected override void ConfigureClient(HttpClient client)
    {
        // Don't follow redirects automatically so tests can verify redirect responses
        client.Timeout = TimeSpan.FromSeconds(30);
        base.ConfigureClient(client);
    }

    public HttpClient CreateNonRedirectingClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = Database.ConnectionString
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Disable anti-forgery token validation for integration tests
            services.AddMvc(options =>
            {
                // Add a filter to ignore anti-forgery tokens in test environment
                options.Filters.Add(new IgnoreAntiforgeryTokenAttribute());
            });
        });

        // Note: Authorization policies and handlers are already configured in Program.cs
        // They will use the actual database roles via UserManager
        // The test authentication scheme is configured per-client in AuthenticationHelper
    }

    public void ResetDatabase()
    {
        Database.Reset();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Database?.Dispose();
        }
        base.Dispose(disposing);
    }
}