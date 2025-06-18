using QuestBoard.Models;

namespace QuestBoard.Repository.Interfaces;

public interface IProposedDateRepository : IGenericRepository<ProposedDate>
{
    Task<IEnumerable<ProposedDate>> GetByQuestIdAsync(int questId);
}