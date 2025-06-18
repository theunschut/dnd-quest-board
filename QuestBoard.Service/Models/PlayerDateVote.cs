using System.ComponentModel.DataAnnotations;

namespace QuestBoard.Service.Models;

public class PlayerDateVote
{
    public int Id { get; set; }

    [Required]
    public int PlayerSignupId { get; set; }

    [Required]
    public int ProposedDateId { get; set; }

    [Required]
    public VoteType Vote { get; set; }

    public virtual PlayerSignup PlayerSignup { get; set; } = null!;
    public virtual ProposedDate ProposedDate { get; set; } = null!;
}