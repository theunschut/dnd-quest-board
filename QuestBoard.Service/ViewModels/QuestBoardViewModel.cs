using QuestBoard.Domain.Models;

namespace QuestBoard.Service.ViewModels;

public class QuestBoardViewModel
{
    public IList<DungeonMaster> DungeonMasters { get; set; } = [];

    public IList<Quest> Quests { get; set; } = [];
}