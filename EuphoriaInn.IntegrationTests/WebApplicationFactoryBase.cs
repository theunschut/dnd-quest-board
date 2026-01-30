using EuphoriaInn.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Builder;

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
            // Replace IAntiforgery with a decorator that validates everything but delegates token generation
            var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Microsoft.AspNetCore.Antiforgery.IAntiforgery));
            if (descriptor != null)
            {
                services.Remove(descriptor);
                services.Add(ServiceDescriptor.Describe(
                    typeof(Microsoft.AspNetCore.Antiforgery.IAntiforgery),
                    sp =>
                    {
                        var inner = ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType!);
                        return new TestAntiforgeryDecorator((Microsoft.AspNetCore.Antiforgery.IAntiforgery)inner);
                    },
                    descriptor.Lifetime));
            }

            // Add test authentication scheme and make it the default
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Identity.Application";
                options.DefaultForbidScheme = "Identity.Application";
            });
        });
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

/// <summary>
/// Decorator for anti-forgery service that delegates token generation to the real service
/// but always succeeds validation (for integration tests)
/// </summary>
public class TestAntiforgeryDecorator : Microsoft.AspNetCore.Antiforgery.IAntiforgery
{
    private readonly Microsoft.AspNetCore.Antiforgery.IAntiforgery _inner;

    public TestAntiforgeryDecorator(Microsoft.AspNetCore.Antiforgery.IAntiforgery inner)
    {
        _inner = inner;
    }

    public Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet GetAndStoreTokens(Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        // Use the real implementation to generate and store tokens
        return _inner.GetAndStoreTokens(httpContext);
    }

    public Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet GetTokens(Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        // Use the real implementation to get tokens
        return _inner.GetTokens(httpContext);
    }

    public Task<bool> IsRequestValidAsync(Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        // Always return true for test requests (skip validation)
        return Task.FromResult(true);
    }

    public void SetCookieTokenAndHeader(Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        // Use the real implementation
        _inner.SetCookieTokenAndHeader(httpContext);
    }

    public Task ValidateRequestAsync(Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        // Always succeed for tests (skip validation)
        return Task.CompletedTask;
    }
}