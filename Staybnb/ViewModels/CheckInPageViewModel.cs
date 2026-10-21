using Staybnb.Models;

namespace Staybnb.ViewModels;

/// <summary>Everything the guest check-in page needs.</summary>
public class CheckInPageViewModel
{
    public Booking Booking { get; set; } = null!;
    public string ProcessTitle { get; set; } = string.Empty;
    public List<string> Steps { get; set; } = new();
    public string RequiredDocuments { get; set; } = "ID or Passport";
    public GuestCheckIn? CheckIn { get; set; }
}
