using AutoMapper;
using EuphoriaInn.Domain.Interfaces;
using EuphoriaInn.Domain.Models.QuestBoard;
using EuphoriaInn.Repository.Entities;
using EuphoriaInn.Repository.Interfaces;

namespace EuphoriaInn.Domain.Services
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

            // Validate: Spectators (SignupRole = 1) cannot vote
            if (playerSignupEntity.SignupRole == 1)
            {
                throw new InvalidOperationException("Spectators cannot vote on dates");
            }

            // Map the new date votes to entities
            var dateVoteEntities = Mapper.Map<List<PlayerDateVoteEntity>>(dateVotes);

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

        public async Task UpdateSignupCharacterAsync(int playerSignupId, int? characterId, CancellationToken cancellationToken = default)
        {
            // Get the existing player signup
            var playerSignupEntity = await repository.GetByIdAsync(playerSignupId, cancellationToken);
            if (playerSignupEntity == null)
            {
                throw new ArgumentException("Player signup not found", nameof(playerSignupId));
            }

            // Update the character ID
            playerSignupEntity.CharacterId = characterId;

            // Update the entity
            await repository.UpdateAsync(playerSignupEntity, cancellationToken);
        }
    }
}