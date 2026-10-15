using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models;

/// <summary>
/// The host's check-in rules for one property (one-to-one with HostProperty).
/// StepsJson holds the steps as JSON text, e.g. ["Upload your ID","Read the house rules"].
/// </summary>
public class CheckInProcess
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public HostProperty Property { get; set; } = null!;

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public string StepsJson { get; set; } = "[]";

    /// <summary>Which documents the guest must upload, e.g. "ID or Passport".</summary>
    [StringLength(200)]
    public string RequiredDocuments { get; set; } = "ID or Passport";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GuestCheckIn> GuestCheckIns { get; set; } = new List<GuestCheckIn>();
}
