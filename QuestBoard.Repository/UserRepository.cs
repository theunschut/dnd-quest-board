using Microsoft.EntityFrameworkCore;
using QuestBoard.Repository.Entities;
using QuestBoard.Repository.Interfaces;

namespace QuestBoard.Repository;

internal class UserRepository(QuestBoardContext context) : BaseRepository<UserEntity>(context), IUserRepository
{
    public virtual async Task<bool> ExistsAsync(string name)
    {
        return await DbSet.AnyAsync(u => u.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
    }
}