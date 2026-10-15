using System.ComponentModel.DataAnnotations;
using Staybnb.Models.Enums;

namespace Staybnb.Models;

/// <summary>
/// A guest's application to become a host. It points at the property (and images)
/// they submitted. When an Admin approves it: add Host role, remove Guest role,
/// and set the property to active.
/// </summary>
public class HostApplication
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;
    public ApplicationUser ApplicationUser { get; set; } = null!;

    public int PropertyId { get; set; }
    public HostProperty Property { get; set; } = null!;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    // Filled in when an Admin reviews it.
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedById { get; set; }

    [StringLength(500)]
    public string? RejectionReason { get; set; }
}
