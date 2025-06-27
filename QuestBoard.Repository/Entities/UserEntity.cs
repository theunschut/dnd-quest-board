using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuestBoard.Repository.Entities;

public class UserEntity : IEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(200)]
    public string? Email { get; set; }

    public string Password { get; set; } = string.Empty;

    public bool IsDungeonMaster { get; set; }

    public virtual ICollection<QuestEntity> Quests { get; set; } = [];

    public ICollection<PlayerSignupEntity> Signups { get; set; } = [];
}