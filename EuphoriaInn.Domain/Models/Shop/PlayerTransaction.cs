using EuphoriaInn.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace EuphoriaInn.Domain.Models.Shop;

public class PlayerTransaction : IModel
{
    public int Id { get; set; }

    [Required]
    public int PlayerId { get; set; }

    public User? Player { get; set; }

    [Required]
    public int ShopItemId { get; set; }

    public ShopItem? ShopItem { get; set; }

    [Required]
    public TransactionType TransactionType { get; set; }

    [Required]
    public decimal Price { get; set; }

    public int Quantity { get; set; } = 1;

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    [StringLength(500)]
    public string? Notes { get; set; }
}