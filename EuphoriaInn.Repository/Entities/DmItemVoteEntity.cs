using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EuphoriaInn.Repository.Entities;

[Table("DmItemVotes")]
public class DmItemVoteEntity : IEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ShopItemId { get; set; }

    [ForeignKey(nameof(ShopItemId))]
    public virtual ShopItemEntity ShopItem { get; set; } = null!;

    [Required]
    public int DmId { get; set; }

    [ForeignKey(nameof(DmId))]
    public virtual UserEntity Dm { get; set; } = null!;

    [Required]
    public int VoteType { get; set; }

    public DateTime VoteDate { get; set; } = DateTime.UtcNow;
}