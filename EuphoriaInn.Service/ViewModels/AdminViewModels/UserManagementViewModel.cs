using EuphoriaInn.Domain.Models;

namespace EuphoriaInn.Service.ViewModels.AdminViewModels;

public class UserManagementViewModel
{
    public User User { get; set; } = new();
    public IList<string> Roles { get; set; } = new List<string>();
    public bool IsAdmin { get; set; }
    public bool IsDungeonMaster { get; set; }
    public bool IsPlayer { get; set; }
}