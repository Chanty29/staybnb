using System.ComponentModel.DataAnnotations;
using Staybnb.Models.Enums;

namespace Staybnb.Models;

/// <summary>
/// Audit trail: every login, role change and booking status update is written here.
/// UserId is optional so system events (or a failed login) can be logged too.
/// </summary>
public class ActivityLog
{
    public int Id { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [Required, StringLength(100)]
    public string Action { get; set; } = string.Empty;

    public ActivityType ActivityType { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
