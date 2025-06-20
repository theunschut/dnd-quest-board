using System.ComponentModel.DataAnnotations;

namespace QuestBoard.Domain.Models;

public class DungeonMaster : IModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(200)]
    public string? Email { get; set; }
}