using Staybnb.Models;

namespace Staybnb.ViewModels;

/// <summary>The numbers and recent items shown on the Admin dashboard.</summary>
public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int PendingApplications { get; set; }
    public int ActiveProperties { get; set; }
    public int TotalBookings { get; set; }
    public int ApprovedHosts { get; set; }

    public List<ActivityLog> RecentActivity { get; set; } = new();
    public List<HostApplication> RecentApplications { get; set; } = new();
}
