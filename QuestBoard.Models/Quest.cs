using System.ComponentModel.DataAnnotations;

namespace QuestBoard.Models;

public class Quest
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public Difficulty Difficulty { get; set; }

    [Required]
    [StringLength(100)]
    public string DmName { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(200)]
    public string? DmEmail { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? FinalizedDate { get; set; }

    public bool IsFinalized { get; set; }

    public virtual ICollection<ProposedDate> ProposedDates { get; set; } = new List<ProposedDate>();
    public virtual ICollection<PlayerSignup> PlayerSignups { get; set; } = new List<PlayerSignup>();
}