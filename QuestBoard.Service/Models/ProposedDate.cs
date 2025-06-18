using System.ComponentModel.DataAnnotations;

namespace QuestBoard.Service.Models;

public class ProposedDate
{
    public int Id { get; set; }

    [Required]
    public int QuestId { get; set; }

    [Required]
    public DateTime Date { get; set; }

    public virtual Quest Quest { get; set; } = null!;
    public virtual ICollection<PlayerDateVote> PlayerVotes { get; set; } = new List<PlayerDateVote>();
}