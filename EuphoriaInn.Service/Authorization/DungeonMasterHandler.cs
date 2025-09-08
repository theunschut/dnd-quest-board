using EuphoriaInn.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace EuphoriaInn.Service.Authorization;

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

        // Check if user is Admin (admins have all DM permissions) or DungeonMaster
        var isAdmin = await userService.IsInRoleAsync(context.User, "Admin");
        var isDungeonMaster = await userService.IsInRoleAsync(context.User, "DungeonMaster");
        
        if (isAdmin || isDungeonMaster)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}