using Microsoft.AspNetCore.Authorization;
using QuestBoard.Domain.Interfaces;

namespace QuestBoard.Service.Authorization;

public class AdminHandler(IUserService userService) : AuthorizationHandler<AdminRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated == true)
        {
            context.Fail();
            return;
        }

        var isAdmin = await userService.IsInRoleAsync(context.User, "Admin");
        
        if (isAdmin)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}