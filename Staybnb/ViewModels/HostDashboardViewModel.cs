using Staybnb.Models;

namespace Staybnb.ViewModels;

public class HostDashboardViewModel
{
    public int TotalProperties { get; set; }
    public int ActiveProperties { get; set; }
    public int PendingRequests { get; set; }
    public int UpcomingStays { get; set; }

    /// <summary>Total of approved, checked-in and completed bookings.</summary>
    public decimal Earnings { get; set; }

    public List<Booking> RecentBookings { get; set; } = new();
}
