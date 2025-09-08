namespace EuphoriaInn.Service.ViewModels.ShopViewModels;

public class ShopIndexViewModel
{
    public IList<ShopItemViewModel> Items { get; set; } = [];
    
    public IList<ShopItemViewModel> EquipmentItems => 
        Items.Where(i => i.Type == EuphoriaInn.Domain.Enums.ItemType.Equipment && i.IsAvailable).ToList();
    
    public IList<ShopItemViewModel> MagicItems => 
        Items.Where(i => i.Type == EuphoriaInn.Domain.Enums.ItemType.MagicItem && i.IsAvailable).ToList();
    
    public IList<ShopItemViewModel> TradeItems => 
        Items.Where(i => i.Type == EuphoriaInn.Domain.Enums.ItemType.TradeItem && i.IsAvailable).ToList();
}