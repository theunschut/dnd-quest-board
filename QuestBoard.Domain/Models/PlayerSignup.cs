using QuestBoard.Domain.Models.Users;
using System.ComponentModel.DataAnnotations;

namespace QuestBoard.Domain.Models;

public class PlayerSignup : IModel
{
    public int Id { get; set; }

    public required Player Player { get; set; }

    public DateTime SignupTime { get; set; } = DateTime.UtcNow;

    public bool IsSelected { get; set; }

    public required Quest Quest { get; set; }

    public IList<PlayerDateVote> DateVotes { get; set; } = [];
}