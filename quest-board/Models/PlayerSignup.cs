using System.ComponentModel.DataAnnotations;

namespace QuestBoard.Models;

public class PlayerSignup
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

    public virtual Quest Quest { get; set; } = null!;
    public virtual ICollection<PlayerDateVote> DateVotes { get; set; } = new List<PlayerDateVote>();
}