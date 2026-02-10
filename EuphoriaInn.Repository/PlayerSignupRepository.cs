using EuphoriaInn.Repository.Entities;
using EuphoriaInn.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EuphoriaInn.Repository;

internal class PlayerSignupRepository(QuestBoardContext context) : BaseRepository<PlayerSignupEntity>(context), IPlayerSignupRepository
{
    public async Task<PlayerSignupEntity?> GetByIdWithDateVotesAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(ps => ps.DateVotes)
            .FirstOrDefaultAsync(ps => ps.Id == id, cancellationToken);
    }

    public async Task ChangeVoteToYesAndSelectAsync(int playerSignupId, int proposedDateId, CancellationToken cancellationToken = default)
    {
        // Get the player signup with its date votes
        var playerSignup = await DbSet
            .Include(ps => ps.DateVotes)
            .FirstOrDefaultAsync(ps => ps.Id == playerSignupId, cancellationToken);

        if (playerSignup == null)
        {
            throw new ArgumentException("Player signup not found", nameof(playerSignupId));
        }

        // Find or create the vote for the proposed date
        var existingVote = playerSignup.DateVotes.FirstOrDefault(dv => dv.ProposedDateId == proposedDateId);

        if (existingVote != null)
        {
            // Update existing vote to Yes
            existingVote.Vote = 0; // VoteType.Yes = 0
        }
        else
        {
            // Create a new Yes vote
            playerSignup.DateVotes.Add(new PlayerDateVoteEntity
            {
                ProposedDateId = proposedDateId,
                PlayerSignupId = playerSignupId,
                Vote = 0 // VoteType.Yes = 0
            });
        }

        // Mark the player as selected
        playerSignup.IsSelected = true;

        // Save changes
        await DbContext.SaveChangesAsync(cancellationToken);
    }
}