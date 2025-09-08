namespace EuphoriaInn.Domain.Models.QuestBoard;

public class PlayerSignup : IModel
{
    public int Id { get; set; }

    public required User Player { get; set; }

    public DateTime SignupTime { get; set; } = DateTime.UtcNow;

    public bool IsSelected { get; set; }

    public required Quest Quest { get; set; }

    public IList<PlayerDateVote> DateVotes { get; set; } = [];
}