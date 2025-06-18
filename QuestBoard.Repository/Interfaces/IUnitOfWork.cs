namespace QuestBoard.Repository.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IQuestRepository Quests { get; }
    IProposedDateRepository ProposedDates { get; }
    IPlayerSignupRepository PlayerSignups { get; }
    IPlayerDateVoteRepository PlayerDateVotes { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}