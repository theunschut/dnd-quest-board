using Hangfire.Dashboard;

namespace EuphoriaInn.Service.Authorization;

public class AdminDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            httpContext.Response.Redirect("/Account/Login");
            return false;
        }

        if (!httpContext.User.IsInRole("Admin"))
        {
            httpContext.Response.Redirect("/Account/Login");
            return false;
        }

        return true;
    }
}
