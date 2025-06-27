using System.ComponentModel.DataAnnotations;

namespace QuestBoard.Domain.Models;

public abstract class User : IModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(200)]
    public string? Email { get; set; }

    [Required]
    [StringLength(100)]
    public string Password { get; set; } = string.Empty;

    public bool IsDungeonMaster { get; set; }

    public IList<Quest> Quests { get; set; } = [];

    public IList<PlayerSignup> Signups { get; set; } = [];
}