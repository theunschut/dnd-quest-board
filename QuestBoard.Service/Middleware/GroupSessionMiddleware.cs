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
///   4. Otherwise, resolve IActiveGroupContext; if ActiveGroupId is null:
///        - GET/HEAD requests are redirected to the hardcoded literal "/groups/pick"
///          (never a user-supplied URL — open-redirect mitigation), preserving the original
///          path+query as ?returnUrl= so GroupPickerController can send the user back
///          (validated there via Url.IsLocalUrl before use).
///        - Non-idempotent requests (POST/PUT/PATCH/DELETE) are NOT redirected, because
///          Response.Redirect (302) causes browsers to re-issue the request as a GET,
///          silently dropping the submitted body with no user-facing error. Instead we
///          short-circuit with 409 Conflict so the caller gets a distinguishable failure
///          signal rather than a silent data loss.
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
            if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
            {
                // Don't silently redirect a non-idempotent request — Response.Redirect emits a
                // 302, which browsers re-issue as a GET, dropping the submitted body with no
                // error shown to the user. Fail loudly instead so the client can surface a
                // "your session expired, please retry" message.
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                return;
            }

            var returnUrl = context.Request.Path + context.Request.QueryString;
            context.Response.Redirect($"/groups/pick?returnUrl={Uri.EscapeDataString(returnUrl)}");
            return;
        }

        await next(context);
    }
}
