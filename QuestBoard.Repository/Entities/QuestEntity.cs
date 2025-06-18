using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuestBoard.Repository.Entities;

[Table("Quests")]
public class QuestEntity
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

    [Required]
    [StringLength(100)]
    public string DmName { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(200)]
    public string? DmEmail { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? FinalizedDate { get; set; }

    public bool IsFinalized { get; set; }

    public virtual ICollection<ProposedDateEntity> ProposedDates { get; set; } = [];

    public virtual ICollection<PlayerSignupEntity> PlayerSignups { get; set; } = [];
}