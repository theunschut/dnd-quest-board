using EuphoriaInn.Domain.Models.QuestBoard;
using System.ComponentModel.DataAnnotations;

namespace EuphoriaInn.Domain.Models;

public class User : IModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(200)]
    public string? Email { get; set; }

    [Required]
    [StringLength(100)]
    public string Password { get; set; } = string.Empty;

    public bool HasKey { get; set; }

    public IList<Quest> Quests { get; set; } = [];

    public IList<PlayerSignup> Signups { get; set; } = [];

    public override bool Equals(object? obj)
    {
        return obj is User user&&
               Id==user.Id&&
               Name==user.Name&&
               Email==user.Email&&
               Password==user.Password&&
               HasKey==user.HasKey;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name, Email, Password, HasKey);
    }
}