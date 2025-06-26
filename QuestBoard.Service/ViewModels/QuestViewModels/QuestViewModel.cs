using QuestBoard.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace QuestBoard.Service.ViewModels.QuestViewModels;

public class QuestViewModel
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public Difficulty Difficulty { get; set; }

    [Required]
    public int DungeonMasterId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one proposed date is required.")]
    public IList<DateTime> ProposedDates { get; set; } = [DateTime.Now.AddDays(1)];
}