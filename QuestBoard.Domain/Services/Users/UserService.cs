using AutoMapper;
using QuestBoard.Domain.Interfaces;
using QuestBoard.Domain.Models;
using QuestBoard.Repository.Entities;
using QuestBoard.Repository.Interfaces;

namespace QuestBoard.Domain.Services.Users;

internal class UserService(IPasswordHashingService hashingService, IUserRepository repository, IMapper mapper) : BaseService<User, UserEntity>(repository, mapper), IUserService
{
    public override Task AddAsync(User model, CancellationToken token = default)
    {
        var hashedPassword = hashingService.HashPassword(model.Password);
        model.Password = hashedPassword;
        return base.AddAsync(model, token);
    }

    public virtual async Task<bool> ExistsAsync(string name)
    {
        return await repository.ExistsAsync(name);
    }

    public override Task UpdateAsync(User model, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}