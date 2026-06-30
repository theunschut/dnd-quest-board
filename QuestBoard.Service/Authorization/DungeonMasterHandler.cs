using Microsoft.AspNetCore.Authorization;
using QuestBoard.Domain.Enums;
using QuestBoard.Domain.Interfaces;

namespace QuestBoard.Service.Authorization;

public class DungeonMasterHandler(
    IUserService userService,
    IActiveGroupContext activeGroupContext)
    : AuthorizationHandler<DungeonMasterRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DungeonMasterRequirement requirement)
    {
        // Step 1: SuperAdmin bypass (D-02)
        if (context.User.IsInRole("SuperAdmin"))
        {
            context.Succeed(requirement);
            return;
        }

        // Step 2: Null group guard (D-03)
        if (activeGroupContext.ActiveGroupId is not { } groupId)
        {
            context.Fail();
            return;
        }

        // Step 3: Group role check (D-04) — DM or Admin both satisfy DungeonMasterRequirement
        var role = await userService.GetGroupRoleAsync(context.User, groupId);
        if (role == GroupRole.Admin || role == GroupRole.DungeonMaster)
            context.Succeed(requirement);
        else
            context.Fail();
    }
}
