using QuestBoard.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace QuestBoard.Service.ViewModels.PlatformViewModels;

public class AddMemberViewModel
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public GroupRole Role { get; set; }
}
