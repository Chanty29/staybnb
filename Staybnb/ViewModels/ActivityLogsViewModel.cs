using Staybnb.Models;
using Staybnb.Models.Enums;

namespace Staybnb.ViewModels;

public class ActivityLogsViewModel
{
    public List<ActivityLog> Items { get; set; } = new();

    /// <summary>The selected type in the filter (null = show all types).</summary>
    public ActivityType? Filter { get; set; }

    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalItems { get; set; }
}
