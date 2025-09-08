using System.ComponentModel.DataAnnotations;
using EuphoriaInn.Domain.Enums;

namespace EuphoriaInn.Domain.Models.Shop;

public class DmItemVote : IModel
{
    public int Id { get; set; }

    [Required]
    public int ShopItemId { get; set; }

    public ShopItem? ShopItem { get; set; }

    [Required]
    public int DmId { get; set; }

    public User? Dm { get; set; }

    [Required]
    public VoteType VoteType { get; set; }

    public DateTime VoteDate { get; set; } = DateTime.UtcNow;
}