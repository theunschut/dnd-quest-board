using QuestBoard.Domain.Models.Users;

namespace QuestBoard.Service.ViewModels.QuestViewModels;

public class CreateQuestViewModel
{
    public QuestViewModel Quest { get; set; } = new();

    public IList<DungeonMaster> DungeonMasters { get; set; } = [];
}