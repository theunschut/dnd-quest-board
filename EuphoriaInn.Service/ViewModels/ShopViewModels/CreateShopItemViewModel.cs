using EuphoriaInn.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace EuphoriaInn.Service.ViewModels.ShopViewModels;

public class CreateShopItemViewModel
{
    [Required]
    [StringLength(200)]
    [Display(Name = "Item Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Item Type")]
    public ItemType Type { get; set; }

    [Required]
    [Display(Name = "Rarity")]
    public ItemRarity Rarity { get; set; }

    [Display(Name = "Quantity (0 = unlimited)")]
    [Range(0, int.MaxValue)]
    public int Quantity { get; set; } = 0;

    [Display(Name = "D&D Beyond Reference URL")]
    [StringLength(500)]
    [Url]
    public string? ReferenceUrl { get; set; }

    [Display(Name = "Available From")]
    [DataType(DataType.DateTime)]
    public DateTime? AvailableFrom { get; set; }

    [Display(Name = "Available Until")]
    [DataType(DataType.DateTime)]
    public DateTime? AvailableUntil { get; set; }
}