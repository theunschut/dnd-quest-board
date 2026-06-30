using QuestBoard.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace QuestBoard.Service.ViewModels.AdminViewModels;

public class CreateUserViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Display Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Group Role")]
    public GroupRole GroupRole { get; set; } = GroupRole.Player;
}
