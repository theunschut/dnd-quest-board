using QuestBoard.Domain.Models;

namespace QuestBoard.Service.ViewModels.CalendarViewModels;

public class QuestOnDay
{
    public Quest Quest { get; set; } = null!;
    public ProposedDate ProposedDate { get; set; } = null!;
    public bool IsFinalized { get; set; }
}