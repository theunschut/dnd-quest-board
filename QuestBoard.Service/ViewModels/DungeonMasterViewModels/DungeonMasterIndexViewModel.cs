using QuestBoard.Domain.Models;

namespace QuestBoard.Service.ViewModels.DungeonMasterViewModels;

public class DungeonMasterIndexViewModel
{
    public IEnumerable<User> DungeonMasters { get; set; } = [];
    public IEnumerable<User> Players { get; set; } = [];
}