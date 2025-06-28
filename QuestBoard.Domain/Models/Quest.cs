using QuestBoard.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace QuestBoard.Domain.Models;

public class Quest : IModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public Difficulty Difficulty { get; set; }

    public int DungeonMasterId { get; set; }

    public User? DungeonMaster { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? FinalizedDate { get; set; }

    public bool IsFinalized { get; set; }

    public int TotalPlayerCount { get; set; }

    public IList<ProposedDate> ProposedDates { get; set; } = [];

    public IList<PlayerSignup> PlayerSignups { get; set; } = [];
}