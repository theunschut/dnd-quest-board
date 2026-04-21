using EuphoriaInn.Domain.Enums;

namespace EuphoriaInn.Service.ViewModels.ShopViewModels;

public class ShopIndexViewModel
{
    public IList<ShopItemViewModel> Items { get; set; } = [];
    public ItemType? SelectedType { get; set; }
    public IList<ItemRarity> SelectedRarities { get; set; } = [];
    public string? SelectedSort { get; set; }
    public bool HasActiveFilters => SelectedRarities.Count > 0 || SelectedSort != null;
    public IList<UserTransactionViewModel> UserPurchases { get; set; } = [];

    public IList<ShopItemViewModel> EquipmentItems =>
        Items.Where(i => i.Type == ItemType.Equipment && i.IsAvailable).ToList();

    public IList<ShopItemViewModel> MagicItems =>
        Items.Where(i => i.Type == ItemType.MagicItem && i.IsAvailable).ToList();
}