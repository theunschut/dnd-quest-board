using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace EuphoriaInn.IntegrationTests.Helpers;

/// <summary>
/// Middleware that selects the Test authentication scheme when a Test authorization header is present
/// </summary>
public class TestAuthSelectorMiddleware
{
    private readonly RequestDelegate _next;

    public TestAuthSelectorMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].ToString();

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Test "))
        {
            // Use Test authentication scheme for this request
            var result = await context.AuthenticateAsync("Test");
            if (result.Succeeded)
            {
                context.User = result.Principal;
            }
        }

        await _next(context);
    }
}
