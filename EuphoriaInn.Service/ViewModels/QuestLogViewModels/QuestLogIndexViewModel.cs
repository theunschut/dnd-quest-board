using EuphoriaInn.Domain.Models.QuestBoard;

namespace EuphoriaInn.Service.ViewModels.QuestLogViewModels;

public class QuestLogIndexViewModel
{
    public IEnumerable<Quest> CompletedQuests { get; set; } = [];
}
