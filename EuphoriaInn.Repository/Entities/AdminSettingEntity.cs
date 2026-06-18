using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EuphoriaInn.Repository.Entities;

[Table("AdminSettings")]
public class AdminSettingEntity  // String PK — does NOT implement IEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [StringLength(200)]
    public string Key { get; set; } = string.Empty;

    public string? Value { get; set; }

    public DateTime UpdatedAt { get; set; }
}
