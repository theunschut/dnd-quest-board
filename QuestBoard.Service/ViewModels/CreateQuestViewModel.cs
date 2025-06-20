using QuestBoard.Domain.Models;

namespace QuestBoard.Service.ViewModels;

public class CreateQuestViewModel
{
    public QuestViewModel Quest { get; set; } = new();

    public IList<DungeonMaster> DungeonMasters { get; set; } = [];
}