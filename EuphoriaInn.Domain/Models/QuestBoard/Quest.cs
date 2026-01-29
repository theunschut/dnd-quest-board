using System.ComponentModel.DataAnnotations;

namespace EuphoriaInn.Domain.Models.QuestBoard;

public class Quest : IModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public int ChallengeRating { get; set; } = 1;

    public int DungeonMasterId { get; set; }

    public User? DungeonMaster { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? FinalizedDate { get; set; }

    public bool IsFinalized { get; set; }

    public int TotalPlayerCount { get; set; }

    public bool DungeonMasterSession { get; set; }

    public string? Recap { get; set; }

    public IList<ProposedDate> ProposedDates { get; set; } = [];

    public IList<PlayerSignup> PlayerSignups { get; set; } = [];
}