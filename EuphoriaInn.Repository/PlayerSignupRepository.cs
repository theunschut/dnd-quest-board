using AutoMapper;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models.QuestBoard;
using EuphoriaInn.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace EuphoriaInn.Repository;

internal class PlayerSignupRepository(QuestBoardContext dbContext, IMapper mapper) : BaseRepository<PlayerSignup, PlayerSignupEntity>(dbContext, mapper), IPlayerSignupRepository
{
    public async Task<PlayerSignup?> GetByIdWithDateVotesAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await DbSet
            .Include(ps => ps.DateVotes)
            .FirstOrDefaultAsync(ps => ps.Id == id, cancellationToken);
        return entity == null ? null : Mapper.Map<PlayerSignup>(entity);
    }

    public async Task ChangeVoteToYesAndSelectAsync(int playerSignupId, int proposedDateId, CancellationToken cancellationToken = default)
    {
        var entity = await DbSet
            .Include(ps => ps.DateVotes)
            .FirstOrDefaultAsync(ps => ps.Id == playerSignupId, cancellationToken);

        if (entity == null)
        {
            throw new ArgumentException("Player signup not found", nameof(playerSignupId));
        }

        var existingVote = entity.DateVotes.FirstOrDefault(dv => dv.ProposedDateId == proposedDateId);

        if (existingVote != null)
        {
            existingVote.Vote = 0; // VoteType.Yes = 0
        }
        else
        {
            entity.DateVotes.Add(new PlayerDateVoteEntity
            {
                ProposedDateId = proposedDateId,
                PlayerSignupId = playerSignupId,
                Vote = 0 // VoteType.Yes = 0
            });
        }

        entity.IsSelected = true;
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    public override async Task UpdateAsync(PlayerSignup model, CancellationToken token = default)
    {
        var entity = await DbSet
            .Include(ps => ps.DateVotes)
            .FirstOrDefaultAsync(ps => ps.Id == model.Id, token);
        if (entity == null) return;

        // Update scalar properties
        entity.IsSelected = model.IsSelected;
        entity.CharacterId = model.CharacterId;
        entity.SignupRole = (int)model.Role;

        // Update date votes
        entity.DateVotes.Clear();
        var dateVoteEntities = Mapper.Map<List<PlayerDateVoteEntity>>(model.DateVotes);
        foreach (var vote in dateVoteEntities)
        {
            entity.DateVotes.Add(vote);
        }

        await DbContext.SaveChangesAsync(token);
    }
}
