using System.ComponentModel.DataAnnotations;
using Staybnb.Models.Enums;

namespace Staybnb.Models;

/// <summary>An ID or passport uploaded by the guest, for the host to verify.</summary>
public class GuestDocument
{
    public int Id { get; set; }

    public int GuestCheckInId { get; set; }
    public GuestCheckIn GuestCheckIn { get; set; } = null!;

    /// <summary>"ID" or "Passport".</summary>
    [Required, StringLength(100)]
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>Name of the saved file inside the uploads folder.</summary>
    [Required, StringLength(200)]
    public string FileName { get; set; } = string.Empty;

    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
