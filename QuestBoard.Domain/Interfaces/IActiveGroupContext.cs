namespace QuestBoard.Domain.Interfaces;

/// <summary>
/// Provides the active group ID for the current request or execution context.
/// Null means "see all records" (Phase 28 temporary state; Phase 29 adds IsSuperAdmin).
/// </summary>
public interface IActiveGroupContext
{
    int? ActiveGroupId { get; }
}
