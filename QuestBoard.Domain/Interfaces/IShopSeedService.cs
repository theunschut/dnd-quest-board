namespace QuestBoard.Domain.Interfaces;

public interface IShopSeedService
{
    /// <summary>
    /// Seeds the shop with a fixed set of basic equipment and magic items, attributed to the given DM.
    /// No-ops if any published item already exists.
    /// </summary>
    Task SeedBasicEquipmentAsync(int createdByUserId);
}