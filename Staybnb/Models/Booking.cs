using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Staybnb.Models.Enums;

namespace Staybnb.Models;

/// <summary>
/// A guest's request to stay at a property.
/// TotalPrice = (PricePerNight x Nights) + CleaningFee + ServiceFee  (calculated in phase 5).
/// </summary>
public class Booking
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public HostProperty Property { get; set; } = null!;

    [Required]
    public string GuestId { get; set; } = string.Empty;
    public ApplicationUser Guest { get; set; } = null!;

    [DataType(DataType.Date)]
    public DateTime CheckInDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime CheckOutDate { get; set; }

    [Range(1, 50)]
    public int NumberOfGuests { get; set; } = 1;

    // The fee amounts are stored on the booking so the price never changes
    // later if the host edits the property.
    [Column(TypeName = "decimal(18,2)")]
    public decimal CleaningFee { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ServiceFee { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Number of nights (not stored in the database).</summary>
    [NotMapped]
    public int Nights => (CheckOutDate.Date - CheckInDate.Date).Days;

    // One-to-one relationships
    public Payment? Payment { get; set; }
    public GuestCheckIn? GuestCheckIn { get; set; }
}
