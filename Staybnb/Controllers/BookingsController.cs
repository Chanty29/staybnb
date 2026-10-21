using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Constants;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.Models.Enums;
using Staybnb.Services;
using Staybnb.ViewModels;

namespace Staybnb.Controllers;

// Guest-only: booking and checking in. Hosts and Admins cannot open these pages.
[Authorize(Roles = AppRoles.Guest)]
public class BookingsController : Controller
{
    private const int MaxNights = 90;
    private const long MaxDocumentBytes = 4 * 1024 * 1024;
    private static readonly string[] AllowedDocumentExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };
    private static readonly string[] DocumentTypes = { "ID", "Passport" };

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogger _activityLogger;
    private readonly INotificationService _notifications;
    private readonly IWebHostEnvironment _env;

    public BookingsController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IActivityLogger activityLogger,
        INotificationService notifications,
        IWebHostEnvironment env)
    {
        _db = db;
        _userManager = userManager;
        _activityLogger = activityLogger;
        _notifications = notifications;
        _env = env;
    }

    // ---------- My bookings ----------

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;

        var bookings = await _db.Bookings
            .Include(b => b.Property).ThenInclude(p => p.Images)
            .Include(b => b.Payment)
            .Include(b => b.GuestCheckIn)
            .Where(b => b.GuestId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return View(bookings);
    }

    // ---------- Booking engine ----------

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingCreateViewModel model)
    {
        var userId = _userManager.GetUserId(User)!;

        var property = await _db.HostProperties
            .FirstOrDefaultAsync(p => p.Id == model.PropertyId && p.IsActive);

        if (property == null)
        {
            return NotFound();
        }

        var checkIn = model.CheckInDate.Date;
        var checkOut = model.CheckOutDate.Date;
        var nights = (checkOut - checkIn).Days;

        string? error = null;

        if (checkIn < DateTime.Today)
        {
            error = "The check-in date cannot be in the past.";
        }
        else if (nights < 1)
        {
            error = "Check-out must be at least one day after check-in.";
        }
        else if (nights > MaxNights)
        {
            error = $"You can book at most {MaxNights} nights at a time.";
        }
        else if (model.NumberOfGuests < 1 || model.NumberOfGuests > property.MaxGuests)
        {
            error = $"This property sleeps between 1 and {property.MaxGuests} guests.";
        }
        else if (property.HostId == userId)
        {
            error = "You cannot book your own property.";
        }
        else
        {
            var taken = await _db.Bookings.AnyAsync(b =>
                b.PropertyId == property.Id
                && (b.Status == BookingStatus.Approved || b.Status == BookingStatus.CheckedIn)
                && b.CheckInDate < checkOut
                && b.CheckOutDate > checkIn);

            if (taken)
            {
                error = "Those dates are already booked. Please choose different dates.";
            }
        }

        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction("Details", "Properties", new { id = property.Id });
        }

        var (_, serviceFee, total) = BookingPricing.Calculate(property.PricePerNight, property.CleaningFee, nights);

        var booking = new Booking
        {
            PropertyId = property.Id,
            GuestId = userId,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            NumberOfGuests = model.NumberOfGuests,
            CleaningFee = property.CleaningFee,
            ServiceFee = serviceFee,
            TotalPrice = total,
            Status = BookingStatus.Pending,
            Payment = new Payment { Amount = total, Status = PaymentStatus.Pending }
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        await _activityLogger.LogAsync(userId, ActivityType.BookingStatusChange,
            $"Booking #{booking.Id} requested for '{property.Title}' ({nights} nights)");

        await _notifications.NotifyAsync(property.HostId,
            "New Booking Request",
            $"A guest requested '{property.Title}' from {checkIn:d MMM} to {checkOut:d MMM yyyy}.",
            NotificationType.BookingRequested);

        TempData["Success"] = "Booking requested. The host will approve or reject it.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = _userManager.GetUserId(User)!;

        var booking = await _db.Bookings
            .Include(b => b.Property)
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == id && b.GuestId == userId);

        if (booking == null)
        {
            return NotFound();
        }

        if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Approved)
        {
            TempData["Error"] = "This booking can no longer be cancelled.";
            return RedirectToAction(nameof(Index));
        }

        booking.Status = BookingStatus.Cancelled;

        if (booking.Payment != null)
        {
            booking.Payment.Status = booking.Payment.Status == PaymentStatus.Paid
                ? PaymentStatus.Refunded
                : PaymentStatus.Failed;
        }

        await _db.SaveChangesAsync();

        await _activityLogger.LogAsync(userId, ActivityType.BookingStatusChange,
            $"Booking #{booking.Id} cancelled by the guest");

        await _notifications.NotifyAsync(booking.Property.HostId,
            "Booking Cancelled",
            $"A guest cancelled their booking for '{booking.Property.Title}'.",
            NotificationType.BookingCancelled);

        TempData["Success"] = "Booking cancelled.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Check-in workflow ----------

    [HttpGet]
    public async Task<IActionResult> CheckIn(int id)
    {
        var userId = _userManager.GetUserId(User)!;

        var booking = await LoadBookingForCheckInAsync(id, userId);
        if (booking == null)
        {
            return NotFound();
        }

        if (booking.Status != BookingStatus.Approved && booking.Status != BookingStatus.CheckedIn)
        {
            TempData["Error"] = "Check-in opens once the host has approved your booking.";
            return RedirectToAction(nameof(Index));
        }

        return View(BuildCheckInPage(booking));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(int id, string? documentType, bool acceptRules, IFormFile? document)
    {
        var userId = _userManager.GetUserId(User)!;

        var booking = await LoadBookingForCheckInAsync(id, userId);
        if (booking == null)
        {
            return NotFound();
        }

        if (booking.Status != BookingStatus.Approved)
        {
            TempData["Error"] = "Check-in is only available for approved bookings.";
            return RedirectToAction(nameof(Index));
        }

        var existing = booking.GuestCheckIn;
        if (existing != null && (existing.Status == CheckInStatus.Submitted || existing.Status == CheckInStatus.Verified))
        {
            TempData["Error"] = "Your check-in was already submitted.";
            return RedirectToAction(nameof(CheckIn), new { id });
        }

        string? error = null;
        var extension = document == null ? string.Empty : Path.GetExtension(document.FileName).ToLowerInvariant();

        if (!acceptRules)
        {
            error = "Please confirm you have read the check-in steps.";
        }
        else if (string.IsNullOrWhiteSpace(documentType) || !DocumentTypes.Contains(documentType))
        {
            error = "Choose whether you are uploading an ID or a Passport.";
        }
        else if (document == null || document.Length == 0)
        {
            error = "Please choose your ID or passport file to upload.";
        }
        else if (!AllowedDocumentExtensions.Contains(extension))
        {
            error = "Only JPG, PNG or PDF files are allowed.";
        }
        else if (document.Length > MaxDocumentBytes)
        {
            error = "The file is too large (4 MB maximum).";
        }

        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(CheckIn), new { id });
        }

        // The host may never have set rules for this property: create the standard ones.
        var process = booking.Property.CheckInProcess;
        if (process == null)
        {
            process = new CheckInProcess
            {
                PropertyId = booking.PropertyId,
                Title = "Standard check-in",
                RequiredDocuments = "ID or Passport",
                StepsJson = JsonSerializer.Serialize(DefaultSteps())
            };
            _db.CheckInProcesses.Add(process);
        }

        // Documents are private: they live outside wwwroot and are served only to the host.
        var folder = Path.Combine(_env.ContentRootPath, "App_Data", "documents");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folder, fileName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await document!.CopyToAsync(stream);
        }

        try
        {
            if (existing == null)
            {
                existing = new GuestCheckIn { BookingId = booking.Id, CheckInProcess = process };
                _db.GuestCheckIns.Add(existing);
            }

            existing.Status = CheckInStatus.Submitted;
            existing.Documents.Add(new GuestDocument
            {
                DocumentType = documentType!,
                FileName = fileName,
                Status = DocumentStatus.Pending
            });

            await _db.SaveChangesAsync();
        }
        catch
        {
            System.IO.File.Delete(fullPath);
            throw;
        }

        await _activityLogger.LogAsync(userId, ActivityType.CheckIn,
            $"Check-in submitted for booking #{booking.Id} ({documentType})");

        await _notifications.NotifyAsync(booking.Property.HostId,
            "Check-in Submitted",
            $"A guest uploaded their {documentType} for '{booking.Property.Title}'. Please verify it.",
            NotificationType.CheckInSubmitted);

        TempData["Success"] = "Check-in submitted. The host will verify your document.";
        return RedirectToAction(nameof(CheckIn), new { id });
    }

    // ---------- helpers ----------

    private Task<Booking?> LoadBookingForCheckInAsync(int id, string userId) =>
        _db.Bookings
            .Include(b => b.Property).ThenInclude(p => p.CheckInProcess)
            .Include(b => b.GuestCheckIn).ThenInclude(c => c!.Documents)
            .FirstOrDefaultAsync(b => b.Id == id && b.GuestId == userId);

    private static CheckInPageViewModel BuildCheckInPage(Booking booking)
    {
        var process = booking.Property.CheckInProcess;
        var steps = process == null ? DefaultSteps() : ReadSteps(process.StepsJson);

        return new CheckInPageViewModel
        {
            Booking = booking,
            ProcessTitle = process?.Title ?? "Standard check-in",
            Steps = steps.Count > 0 ? steps : DefaultSteps(),
            RequiredDocuments = process?.RequiredDocuments ?? "ID or Passport",
            CheckIn = booking.GuestCheckIn
        };
    }

    private static List<string> DefaultSteps() => new()
    {
        "Confirm your arrival time",
        "Upload your ID or passport",
        "Read and accept the house rules"
    };

    private static List<string> ReadSteps(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }
}
