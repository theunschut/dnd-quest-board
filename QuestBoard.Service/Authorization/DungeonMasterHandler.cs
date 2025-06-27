using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using QuestBoard.Repository.Entities;

namespace QuestBoard.Service.Authorization;

public class DungeonMasterHandler : AuthorizationHandler<DungeonMasterRequirement>
{
    private readonly UserManager<UserEntity> _userManager;

    public DungeonMasterHandler(UserManager<UserEntity> userManager)
    {
        _userManager = userManager;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DungeonMasterRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated == true)
        {
            context.Fail();
            return;
        }

        var user = await _userManager.GetUserAsync(context.User);
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