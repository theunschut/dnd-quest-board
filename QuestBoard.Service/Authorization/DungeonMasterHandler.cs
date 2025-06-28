using Microsoft.AspNetCore.Authorization;
using QuestBoard.Domain.Interfaces;

namespace QuestBoard.Service.Authorization;

public class DungeonMasterHandler(IUserService userService) : AuthorizationHandler<DungeonMasterRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DungeonMasterRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated == true)
        {
            context.Fail();
            return;
        }

        var user = await userService.GetUserAsync(context.User);
        if (user?.IsDungeonMaster == true)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}