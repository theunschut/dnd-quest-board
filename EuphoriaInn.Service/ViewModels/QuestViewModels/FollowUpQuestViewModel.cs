using System.ComponentModel.DataAnnotations;

namespace EuphoriaInn.Service.ViewModels.QuestViewModels;

public class FollowUpQuestViewModel
{
    /// <summary>Id of the original quest this is a follow-up to (D-14).</summary>
    [Required]
    public int OriginalQuestId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(1, 20, ErrorMessage = "Challenge Rating must be between 1 and 20.")]
    public int ChallengeRating { get; set; } = 1;

    [Required]
    public int DungeonMasterId { get; set; }

    public int TotalPlayerCount { get; set; } = 6;

    /// <summary>Always false for new follow-up quests (D-04).</summary>
    public bool DungeonMasterSession { get; set; } = false;

    /// <summary>
    /// Must contain at least one date before saving (D-03, FOLLOW-03).
    /// No default date — DM must add dates explicitly.
    /// Custom error message per UI-SPEC copywriting contract.
    /// </summary>
    [MinLength(1, ErrorMessage = "At least one proposed date is required before saving a follow-up quest.")]
    public IList<DateTime> ProposedDates { get; set; } = [];
}
