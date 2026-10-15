using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Staybnb.Models.Enums;

namespace Staybnb.Models;

/// <summary>
/// A property listed on Staybnb. It starts inactive when a guest applies to host,
/// and becomes active when an Admin approves the HostApplication.
/// </summary>
public class HostProperty
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    [Range(typeof(decimal), "1", "1000000", ErrorMessage = "Price per night must be at least 1.")]
    [Display(Name = "Price per night")]
    public decimal PricePerNight { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(typeof(decimal), "0", "1000000")]
    [Display(Name = "Cleaning fee")]
    public decimal CleaningFee { get; set; }

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [StringLength(100)]
    public string Country { get; set; } = "South Africa";

    [Display(Name = "Property type")]
    public PropertyType PropertyType { get; set; }

    [Range(1, 50)]
    [Display(Name = "Max guests")]
    public int MaxGuests { get; set; } = 1;

    [Range(0, 50)]
    public int Bedrooms { get; set; }

    [Range(0, 50)]
    public int Beds { get; set; }

    [Range(0, 50)]
    public int Bathrooms { get; set; }

    /// <summary>Hosts toggle this on the dashboard. Only active properties show in the gallery.</summary>
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ----- Relationships -----

    [Required]
    public string HostId { get; set; } = string.Empty;
    public ApplicationUser Host { get; set; } = null!;

    public ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();
    public ICollection<Amenity> Amenities { get; set; } = new List<Amenity>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<HostApplication> Applications { get; set; } = new List<HostApplication>();

    /// <summary>One-to-one: the check-in rules the host configured for this property.</summary>
    public CheckInProcess? CheckInProcess { get; set; }
}
