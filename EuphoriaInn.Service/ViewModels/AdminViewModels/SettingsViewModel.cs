using System.ComponentModel.DataAnnotations;

namespace EuphoriaInn.Service.ViewModels.AdminViewModels;

public class SettingsViewModel
{
    [DisplayFormat(ConvertEmptyStringToNull = true)]
    [Url]
    [StringLength(2000)]
    [Display(Name = "Omphalos URL")]
    public string? OmphalosUrl { get; set; }

    [StringLength(500)]
    [Display(Name = "Shared Secret")]
    public string? OmphalosSharedSecret { get; set; }  // No [Required] — blank = preserve existing (D-08)

    [Display(Name = "Enable Omphalos integration")]
    public bool IsEnabled { get; set; }
}
