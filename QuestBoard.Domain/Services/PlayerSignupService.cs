using AutoMapper;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Models;
using QuestBoard.Repository.Entities;
using QuestBoard.Repository.Interfaces;

namespace QuestBoard.Domain.Services
{
    internal class PlayerSignupService(IPlayerSignupRepository repository, IMapper mapper) : BaseService<PlayerSignup, PlayerSignupEntity>(repository, mapper), IPlayerSignupService
    {
        public override Task UpdateAsync(PlayerSignup model, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }
    }
}