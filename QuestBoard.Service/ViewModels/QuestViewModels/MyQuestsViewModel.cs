using QuestBoard.Domain.Models;

namespace QuestBoard.Service.ViewModels.QuestViewModels;

public class MyQuestsViewModel
{
    public IEnumerable<Quest> Open { get; set; } = [];

    public IEnumerable<Quest> Finalized { get; set; } = [];

    public IEnumerable<Quest> Done { get; set; } = [];
}