using QuestBoard.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace QuestBoard.Domain.Models;

public class PlayerDateVote
{
    public int Id { get; set; }

    [Required]
    public int PlayerSignupId { get; set; }

    [Required]
    public int ProposedDateId { get; set; }

    [Required]
    public VoteType Vote { get; set; }

    public PlayerSignup? PlayerSignup { get; set; }
    public ProposedDate? ProposedDate { get; set; }
}