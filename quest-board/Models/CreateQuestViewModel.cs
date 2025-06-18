using System.ComponentModel.DataAnnotations;

namespace QuestBoard.Models;

public class CreateQuestViewModel
{
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

    [Required]
    [MinLength(1, ErrorMessage = "At least one proposed date is required.")]
    public List<DateTime> ProposedDates { get; set; } = new List<DateTime> { DateTime.Now.AddDays(1) };
}