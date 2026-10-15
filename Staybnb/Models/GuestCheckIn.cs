using Staybnb.Models.Enums;

namespace Staybnb.Models;

/// <summary>One guest's check-in for one booking (one-to-one with Booking).</summary>
public class GuestCheckIn
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public int CheckInProcessId { get; set; }
    public CheckInProcess CheckInProcess { get; set; } = null!;

    public CheckInStatus Status { get; set; } = CheckInStatus.InProgress;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GuestDocument> Documents { get; set; } = new List<GuestDocument>();
}
