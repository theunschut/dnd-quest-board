using EuphoriaInn.Domain.Models;

namespace EuphoriaInn.Service.ViewModels.GuildMembersViewModels;

public class GuildMembersIndexViewModel
{
    public IEnumerable<User> DungeonMasters { get; set; } = [];
    public IEnumerable<User> Players { get; set; } = [];
}