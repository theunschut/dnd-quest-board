using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using QuestBoard.Domain.Interfaces;

namespace QuestBoard.Service.Middleware;

/// <summary>
/// Session-recovery middleware (D-09, D-10, D-11). Redirects an authenticated user whose
/// group session has expired (no ActiveGroupId) to the group picker instead of letting the
/// request fall through to a broken, group-scoped page.
///
/// Guard order matters:
///   1. Anonymous requests pass through — [Authorize] handles the login redirect.
///   2. SuperAdmin passes through — a null ActiveGroupId is correct by design and must be
///      checked BEFORE the group check to avoid a redirect loop.
///   3. Exempt paths (the picker itself, auth, platform, error routes) pass through.
///   4. Otherwise, resolve IActiveGroupContext; if ActiveGroupId is null, redirect to the
///      hardcoded literal "/groups/pick" (never a user-supplied URL — open-redirect mitigation).
/// </summary>
public class GroupSessionMiddleware(RequestDelegate next)
{
    private static readonly string[] ExemptPathPrefixes =
        ["/groups/pick", "/GroupPicker", "/Account", "/platform", "/Error"];

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        if (context.User.IsInRole("SuperAdmin"))
        {
            await next(context);
            return;
        }

        if (ExemptPathPrefixes.Any(prefix => context.Request.Path.StartsWithSegments(prefix)))
        {
            await next(context);
            return;
        }

        var groupContext = context.RequestServices.GetRequiredService<IActiveGroupContext>();
        if (groupContext.ActiveGroupId == null)
        {
            context.Response.Redirect("/groups/pick");
            return;
        }

        await next(context);
    }
}
