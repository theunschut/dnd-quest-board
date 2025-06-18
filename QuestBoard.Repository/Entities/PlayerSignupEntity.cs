using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuestBoard.Repository.Entities;

[Table("PlayerSignups")]
public class PlayerSignupEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int QuestId { get; set; }

    [Required]
    [StringLength(100)]
    public string PlayerName { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(200)]
    public string? PlayerEmail { get; set; }

    public DateTime SignupTime { get; set; } = DateTime.UtcNow;

    public bool IsSelected { get; set; }

    [ForeignKey(nameof(QuestId))]
    public virtual QuestEntity Quest { get; set; } = null!;

    public virtual ICollection<PlayerDateVoteEntity> DateVotes { get; set; } = [];
}