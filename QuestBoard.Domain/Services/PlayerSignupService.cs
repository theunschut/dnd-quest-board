using AutoMapper;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Models;
using QuestBoard.Repository.Entities;
using QuestBoard.Repository.Interfaces;

namespace QuestBoard.Domain.Services
{
    internal class PlayerSignupService(IPlayerSignupRepository repository, IMapper mapper) : BaseService<PlayerSignup, PlayerSignupEntity>(repository, mapper), IPlayerSignupService
    {
        public async Task UpdatePlayerDateVotesAsync(int playerSignupId, List<PlayerDateVote> dateVotes, CancellationToken cancellationToken = default)
        {
            // Get the existing player signup with its date votes
            var playerSignupEntity = await repository.GetByIdWithDateVotesAsync(playerSignupId, cancellationToken);
            if (playerSignupEntity == null)
            {
                throw new ArgumentException("Player signup not found", nameof(playerSignupId));
            }

            // Map the new date votes to entities
            var dateVoteEntities = mapper.Map<List<PlayerDateVoteEntity>>(dateVotes);
            
            // Set the player signup ID for all date votes
            foreach (var vote in dateVoteEntities)
            {
                vote.PlayerSignupId = playerSignupId;
            }

            // Clear existing date votes and add new ones
            playerSignupEntity.DateVotes.Clear();
            foreach (var vote in dateVoteEntities)
            {
                playerSignupEntity.DateVotes.Add(vote);
            }

            // Update the entity
            await repository.UpdateAsync(playerSignupEntity, cancellationToken);
        }
    }
}