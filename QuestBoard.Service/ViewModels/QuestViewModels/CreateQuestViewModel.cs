using QuestBoard.Domain.Models;

namespace QuestBoard.Service.ViewModels.QuestViewModels;

public class CreateQuestViewModel
{
    public QuestViewModel Quest { get; set; } = new();

    public IList<User> DungeonMasters { get; set; } = [];
}