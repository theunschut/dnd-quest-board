using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuestBoard.Repository.Entities;

[Table("Quests")]
public class QuestEntity : IEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int Difficulty { get; set; }

    public int DungeonMasterId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? FinalizedDate { get; set; }

    public bool IsFinalized { get; set; }

    [ForeignKey(nameof(DungeonMasterId))]
    public virtual DungeonMasterEntity DungeonMaster { get; set; } = null!;

    public virtual ICollection<ProposedDateEntity> ProposedDates { get; set; } = [];

    public virtual ICollection<PlayerSignupEntity> PlayerSignups { get; set; } = [];
}