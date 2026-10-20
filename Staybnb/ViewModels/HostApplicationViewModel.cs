using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Staybnb.Models;
using Staybnb.Models.Enums;

namespace Staybnb.ViewModels;

/// <summary>
/// The "Become a Host" form: the property details plus the photos.
/// A view model keeps the form separate from the database model.
/// </summary>
public class HostApplicationViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required, StringLength(200)]
    [Display(Name = "Street address")]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Country { get; set; } = "South Africa";

    [Display(Name = "Property type")]
    public PropertyType PropertyType { get; set; }

    [Range(typeof(decimal), "1", "1000000", ErrorMessage = "Price per night must be at least 1.")]
    [Display(Name = "Price per night (R)")]
    public decimal PricePerNight { get; set; }

    [Range(typeof(decimal), "0", "1000000", ErrorMessage = "Cleaning fee cannot be negative.")]
    [Display(Name = "Cleaning fee (R)")]
    public decimal CleaningFee { get; set; }

    [Range(1, 50)]
    [Display(Name = "Max guests")]
    public int MaxGuests { get; set; } = 1;

    [Range(0, 50)]
    public int Bedrooms { get; set; } = 1;

    [Range(0, 50)]
    public int Beds { get; set; } = 1;

    [Range(0, 50)]
    public int Bathrooms { get; set; } = 1;

    /// <summary>Ids of the amenities the host ticked.</summary>
    public List<int> SelectedAmenityIds { get; set; } = new();

    /// <summary>The full amenity list for the checkboxes. Filled by the controller, not posted.</summary>
    public List<Amenity> AllAmenities { get; set; } = new();

    /// <summary>The uploaded photos. At least one is required (checked in the controller).</summary>
    public List<IFormFile> Images { get; set; } = new();
}
