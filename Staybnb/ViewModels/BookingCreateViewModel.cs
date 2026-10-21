using System.ComponentModel.DataAnnotations;

namespace Staybnb.ViewModels;

/// <summary>What the guest posts from the property details page.</summary>
public class BookingCreateViewModel
{
    public int PropertyId { get; set; }

    [DataType(DataType.Date)]
    public DateTime CheckInDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime CheckOutDate { get; set; }

    public int NumberOfGuests { get; set; } = 1;
}
