using Microsoft.EntityFrameworkCore.Storage;
using QuestBoard.Repository.Interfaces;
using QuestBoard.Data;

namespace QuestBoard.Repository.Implementations;

public class UnitOfWork : IUnitOfWork
{
    private readonly QuestBoardContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(QuestBoardContext context)
    {
        _context = context;
        Quests = new QuestRepository(_context);
        ProposedDates = new ProposedDateRepository(_context);
        PlayerSignups = new PlayerSignupRepository(_context);
        PlayerDateVotes = new PlayerDateVoteRepository(_context);
    }

    public IQuestRepository Quests { get; }
    public IProposedDateRepository ProposedDates { get; }
    public IPlayerSignupRepository PlayerSignups { get; }
    public IPlayerDateVoteRepository PlayerDateVotes { get; }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}