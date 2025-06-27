using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuestBoard.Repository.Entities;

[Table("PlayerSignups")]
public class PlayerSignupEntity : IEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [ForeignKey(nameof(QuestId))]
    public bool IsSelected { get; set; }

    [Required]
    public int PlayerId { get; set; }

    public DateTime SignupTime { get; set; } = DateTime.UtcNow;

    [Required]
    public int QuestId { get; set; }

    [ForeignKey(nameof(PlayerId))]
    public UserEntity Player { get; set; } = null!;

    [ForeignKey(nameof(QuestId))]
    public virtual QuestEntity Quest { get; set; } = null!;

    public virtual ICollection<PlayerDateVoteEntity> DateVotes { get; set; } = [];
}