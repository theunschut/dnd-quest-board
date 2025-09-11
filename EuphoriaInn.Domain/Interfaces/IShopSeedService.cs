namespace EuphoriaInn.Domain.Interfaces;

public interface IShopSeedService
{
    Task SeedBasicEquipmentAsync(int createdByUserId);
}