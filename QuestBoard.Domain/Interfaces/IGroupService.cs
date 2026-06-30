using QuestBoard.Domain.Enums;
using QuestBoard.Domain.Models;

namespace QuestBoard.Domain.Interfaces;

public interface IGroupService : IBaseService<Group>
{
    Task<IList<GroupWithMemberCount>> GetAllWithMemberCountAsync(CancellationToken token = default);
    Task<IList<GroupWithMemberCount>> GetGroupsForUserAsync(int userId, CancellationToken token = default);
    Task<bool> HasMembersAsync(int groupId, CancellationToken token = default);
    Task AddMemberAsync(int groupId, int userId, GroupRole groupRole, CancellationToken token = default);
    Task RemoveMemberAsync(int groupId, int userId, CancellationToken token = default);
    Task<IList<UserGroup>> GetMembersAsync(int groupId, CancellationToken token = default);
}
