using Microsoft.EntityFrameworkCore;
using Staybnb.Models;

namespace Staybnb.Data;

/// <summary>
/// Adds the standard amenities (Wifi, Kitchen...) the first time the app runs,
/// so hosts have something to tick on the application form. Safe to run many times.
/// </summary>
public static class AmenitySeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Amenities.AnyAsync())
        {
            return;
        }

        context.Amenities.AddRange(
            new Amenity { Name = "Wifi", IconClass = "bi bi-wifi" },
            new Amenity { Name = "Kitchen", IconClass = "bi bi-egg-fried" },
            new Amenity { Name = "Washer", IconClass = "bi bi-droplet" },
            new Amenity { Name = "Dryer", IconClass = "bi bi-wind" },
            new Amenity { Name = "Air conditioning", IconClass = "bi bi-snow" },
            new Amenity { Name = "Heating", IconClass = "bi bi-thermometer-sun" },
            new Amenity { Name = "Pool", IconClass = "bi bi-water" },
            new Amenity { Name = "Free parking", IconClass = "bi bi-p-circle" },
            new Amenity { Name = "TV", IconClass = "bi bi-tv" },
            new Amenity { Name = "Braai area", IconClass = "bi bi-fire" },
            new Amenity { Name = "Backup power", IconClass = "bi bi-lightning-charge" },
            new Amenity { Name = "Security", IconClass = "bi bi-shield-check" });

        await context.SaveChangesAsync();
    }
}
