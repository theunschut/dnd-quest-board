using System.ComponentModel.DataAnnotations;

namespace QuestBoard.Service.ViewModels.DungeonMasterViewModels;

public class CreateDungeonMasterViewModel
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(200)]
    public string? Email { get; set; }

    [Required]
    [StringLength(100)]
    public string Password { get; set; } = string.Empty;
}