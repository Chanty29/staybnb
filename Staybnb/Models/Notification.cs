using System.ComponentModel.DataAnnotations;
using Staybnb.Models.Enums;

namespace Staybnb.Models;

/// <summary>The bell icon: "Booking Approved", "New Message", etc.</summary>
public class Notification
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [Required, StringLength(100)]
    public string Title { get; set; } = string.Empty;

    /// <summary>The longer text shown when the notification is opened.</summary>
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; }

    /// <summary>False until the user opens it.</summary>
    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
