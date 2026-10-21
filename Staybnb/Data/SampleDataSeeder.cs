using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Staybnb.Constants;
using Staybnb.Models;
using Staybnb.Models.Enums;
using Staybnb.Services;

namespace Staybnb.Data;

/// <summary>
/// Adds demo data the first time the app runs so the gallery is not empty:
///   host@staybnb.com  / Host@12345   (Host, with 4 approved, active properties)
///   guest@staybnb.com / Guest@12345  (Guest, with two bookings)
///   admin@staybnb.com / Admin@12345  (Admin)
/// Safe to run many times: it does nothing if host@staybnb.com already exists.
/// </summary>
public static class SampleDataSeeder
{
    private const string HostEmail = "host@staybnb.com";
    private const string GuestEmail = "guest@staybnb.com";
    private const string AdminEmail = "admin@staybnb.com";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        if (await userManager.FindByEmailAsync(HostEmail) != null)
        {
            return;
        }

        var host = await CreateUserAsync(userManager, HostEmail, "Host@12345", "Thabo", "Mokoena", AppRoles.Host);
        var guest = await CreateUserAsync(userManager, GuestEmail, "Guest@12345", "Lerato", "Dlamini", AppRoles.Guest);
        await CreateUserAsync(userManager, AdminEmail, "Admin@12345", "Anele", "Naidoo", AppRoles.Admin);

        var amenities = await db.Amenities.OrderBy(a => a.Id).Take(6).ToListAsync();

        var properties = new List<HostProperty>
        {
            Build(host, "Sea-view apartment in Camps Bay", "Bright two-bedroom apartment a short walk from the beach, with a balcony and fast wifi.",
                1450, 250, "12 Victoria Road", "Cape Town", PropertyType.Apartment, 4, 2, 3, 1, "camps-bay", amenities),
            Build(host, "Cosy Soweto guest house", "A warm family-run house close to Vilakazi Street. Breakfast available on request.",
                780, 150, "45 Vilakazi Street", "Johannesburg", PropertyType.House, 5, 3, 4, 2, "soweto-house", amenities),
            Build(host, "Drakensberg mountain cabin", "A wooden cabin with a fireplace, braai area and views of the Drakensberg.",
                990, 200, "Bergview Farm Road", "Underberg", PropertyType.Cabin, 6, 3, 4, 1, "drakensberg-cabin", amenities),
            Build(host, "Durban beachfront room", "A private room with its own bathroom, steps from the promenade.",
                520, 100, "88 Marine Parade", "Durban", PropertyType.Room, 2, 1, 1, 1, "durban-room", amenities)
        };

        db.HostProperties.AddRange(properties);
        await db.SaveChangesAsync();

        // Each demo property went through the normal approval flow.
        foreach (var p in properties)
        {
            db.HostApplications.Add(new HostApplication
            {
                ApplicationUserId = host.Id,
                PropertyId = p.Id,
                Status = ApplicationStatus.Approved,
                AppliedAt = DateTime.UtcNow.AddDays(-14),
                ReviewedAt = DateTime.UtcNow.AddDays(-13)
            });
        }

        // Two bookings for the demo guest: one waiting for the host, one approved (ready for check-in).
        db.Bookings.Add(NewBooking(properties[0], guest, 10, 13, 2, BookingStatus.Pending));
        db.Bookings.Add(NewBooking(properties[1], guest, 20, 22, 3, BookingStatus.Approved));

        await db.SaveChangesAsync();

        db.Notifications.Add(new Notification
        {
            UserId = host.Id,
            Title = "New Booking Request",
            Message = "A guest requested 'Sea-view apartment in Camps Bay'.",
            Type = NotificationType.BookingRequested
        });
        db.Notifications.Add(new Notification
        {
            UserId = guest.Id,
            Title = "Booking Approved",
            Message = "Your booking for 'Cosy Soweto guest house' was approved. You can now complete your check-in.",
            Type = NotificationType.BookingApproved
        });
        db.Messages.Add(new Message
        {
            SenderId = guest.Id,
            ReceiverId = host.Id,
            Content = "Hi! Is early check-in possible for my stay?",
            Timestamp = DateTime.UtcNow.AddHours(-3)
        });

        db.ActivityLogs.Add(new ActivityLog
        {
            UserId = host.Id,
            ActivityType = ActivityType.System,
            Action = "Sample data created",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> CreateUserAsync(
        UserManager<ApplicationUser> userManager, string email, string password,
        string firstName, string lastName, string role)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Could not create sample user " + email + ": " + string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    private static HostProperty Build(
        ApplicationUser host, string title, string description, decimal price, decimal cleaning,
        string address, string city, PropertyType type, int maxGuests, int bedrooms, int beds, int bathrooms,
        string imageSeed, List<Amenity> amenities)
    {
        var property = new HostProperty
        {
            Title = title,
            Description = description,
            PricePerNight = price,
            CleaningFee = cleaning,
            Address = address,
            City = city,
            Country = "South Africa",
            PropertyType = type,
            MaxGuests = maxGuests,
            Bedrooms = bedrooms,
            Beds = beds,
            Bathrooms = bathrooms,
            IsActive = true,
            HostId = host.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-14)
        };

        property.Images.Add(new PropertyImage { ImageUrl = $"https://picsum.photos/seed/{imageSeed}-1/900/600" });
        property.Images.Add(new PropertyImage { ImageUrl = $"https://picsum.photos/seed/{imageSeed}-2/900/600" });

        foreach (var a in amenities)
        {
            property.Amenities.Add(a);
        }

        return property;
    }

    private static Booking NewBooking(HostProperty property, ApplicationUser guest, int startInDays, int endInDays, int guests, BookingStatus status)
    {
        var checkIn = DateTime.Today.AddDays(startInDays);
        var checkOut = DateTime.Today.AddDays(endInDays);
        var nights = (checkOut - checkIn).Days;
        var (_, serviceFee, total) = BookingPricing.Calculate(property.PricePerNight, property.CleaningFee, nights);

        return new Booking
        {
            PropertyId = property.Id,
            GuestId = guest.Id,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            NumberOfGuests = guests,
            CleaningFee = property.CleaningFee,
            ServiceFee = serviceFee,
            TotalPrice = total,
            Status = status,
            Payment = new Payment { Amount = total, Status = PaymentStatus.Pending }
        };
    }
}
