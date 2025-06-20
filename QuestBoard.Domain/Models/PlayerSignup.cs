using System.ComponentModel.DataAnnotations;

namespace QuestBoard.Domain.Models;

public class PlayerSignup : IModel
{
    public int Id { get; set; }

    [Required]
    public int QuestId { get; set; }

    [Required]
    [StringLength(100)]
    public string PlayerName { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(200)]
    public string? PlayerEmail { get; set; }

    public DateTime SignupTime { get; set; } = DateTime.UtcNow;

    public bool IsSelected { get; set; }

    public Quest? Quest { get; set; }

    public IList<PlayerDateVote> DateVotes { get; set; } = [];
}