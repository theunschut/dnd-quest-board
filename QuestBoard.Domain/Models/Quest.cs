using QuestBoard.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace QuestBoard.Domain.Models;

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

    public IList<ProposedDate> ProposedDates { get; set; } = [];

    public IList<PlayerSignup> PlayerSignups { get; set; } = [];
}