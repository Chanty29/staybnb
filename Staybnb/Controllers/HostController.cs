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

// Host-only area. Guests cannot open this until an Admin approves their application.
[Authorize(Roles = AppRoles.Host)]
public class HostController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogger _activityLogger;
    private readonly INotificationService _notifications;

    public HostController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IActivityLogger activityLogger,
        INotificationService notifications)
    {
        _db = db;
        _userManager = userManager;
        _activityLogger = activityLogger;
        _notifications = notifications;
    }

    // ---------- Dashboard ----------

    public async Task<IActionResult> Dashboard()
    {
        var userId = _userManager.GetUserId(User)!;
        var today = DateTime.UtcNow.Date;

        var myBookings = _db.Bookings.Where(b => b.Property.HostId == userId);

        var model = new HostDashboardViewModel
        {
            TotalProperties = await _db.HostProperties.CountAsync(p => p.HostId == userId),
            ActiveProperties = await _db.HostProperties.CountAsync(p => p.HostId == userId && p.IsActive),
            PendingRequests = await myBookings.CountAsync(b => b.Status == BookingStatus.Pending),
            UpcomingStays = await myBookings.CountAsync(b =>
                b.Status == BookingStatus.Approved && b.CheckInDate >= today),
            Earnings = await myBookings
                .Where(b => b.Status == BookingStatus.Approved
                         || b.Status == BookingStatus.CheckedIn
                         || b.Status == BookingStatus.Completed)
                .SumAsync(b => b.TotalPrice),
            RecentBookings = await myBookings
                .Include(b => b.Property)
                .Include(b => b.Guest)
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .ToListAsync()
        };

        return View(model);
    }

    // ---------- My properties ----------

    public async Task<IActionResult> Properties()
    {
        var userId = _userManager.GetUserId(User)!;

        var properties = await _db.HostProperties
            .Include(p => p.Images)
            .Include(p => p.Applications)
            .Include(p => p.Bookings)
            .Where(p => p.HostId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(properties);
    }

    /// <summary>The host switches a listing on or off (IsActive). Only approved properties can be switched.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var userId = _userManager.GetUserId(User)!;

        // Only the owner may change their own property.
        var property = await _db.HostProperties
            .Include(p => p.Applications)
            .FirstOrDefaultAsync(p => p.Id == id && p.HostId == userId);

        if (property == null)
        {
            return NotFound();
        }

        var canToggle = !property.Applications.Any()
                        || property.Applications.Any(a => a.Status == ApplicationStatus.Approved);

        if (!canToggle)
        {
            TempData["Error"] = "This property can only be switched on after an Admin approves it.";
            return RedirectToAction(nameof(Properties));
        }

        property.IsActive = !property.IsActive;
        await _db.SaveChangesAsync();

        var state = property.IsActive ? "Activated" : "Deactivated";
        await _activityLogger.LogAsync(userId, ActivityType.PropertyUpdate, $"{state} property '{property.Title}'");

        TempData["Success"] = $"'{property.Title}' is now {(property.IsActive ? "active" : "inactive")}.";
        return RedirectToAction(nameof(Properties));
    }

    // ---------- Booking requests ----------

    public async Task<IActionResult> Bookings(BookingStatus? status)
    {
        var userId = _userManager.GetUserId(User)!;

        var query = _db.Bookings
            .Include(b => b.Property)
            .Include(b => b.Guest)
            .Where(b => b.Property.HostId == userId);

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        var bookings = await query
            .OrderBy(b => b.Status == BookingStatus.Pending ? 0 : 1)   // pending requests first
            .ThenByDescending(b => b.CreatedAt)
            .ToListAsync();

        ViewData["Filter"] = status;
        return View(bookings);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> ApproveBooking(int id) => ChangeBookingStatusAsync(id, BookingStatus.Approved);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> RejectBooking(int id) => ChangeBookingStatusAsync(id, BookingStatus.Rejected);

    private async Task<IActionResult> ChangeBookingStatusAsync(int id, BookingStatus newStatus)
    {
        var userId = _userManager.GetUserId(User)!;

        // The booking must belong to one of THIS host's properties.
        var booking = await _db.Bookings
            .Include(b => b.Property)
            .Include(b => b.Guest)
            .FirstOrDefaultAsync(b => b.Id == id && b.Property.HostId == userId);

        if (booking == null)
        {
            return NotFound();
        }

        if (booking.Status != BookingStatus.Pending)
        {
            TempData["Error"] = "Only pending requests can be approved or rejected.";
            return RedirectToAction(nameof(Bookings));
        }

        booking.Status = newStatus;
        await _db.SaveChangesAsync();

        var approved = newStatus == BookingStatus.Approved;
        var title = booking.Property.Title;

        await _activityLogger.LogAsync(userId, ActivityType.BookingStatusChange,
            $"Booking #{booking.Id} {(approved ? "approved" : "rejected")} for '{title}'");

        await _notifications.NotifyAsync(booking.GuestId,
            approved ? "Booking Approved" : "Booking Rejected",
            approved
                ? $"Your booking for '{title}' was approved. You can now complete your check-in."
                : $"Your booking request for '{title}' was rejected by the host.",
            approved ? NotificationType.BookingApproved : NotificationType.BookingRejected);

        TempData["Success"] = approved ? "Booking approved." : "Booking rejected.";
        return RedirectToAction(nameof(Bookings));
    }

    // ---------- Check-in rules for a property ----------

    [HttpGet]
    public async Task<IActionResult> CheckIn(int id)
    {
        var userId = _userManager.GetUserId(User)!;

        var property = await _db.HostProperties.FirstOrDefaultAsync(p => p.Id == id && p.HostId == userId);
        if (property == null)
        {
            return NotFound();
        }

        var existing = await _db.CheckInProcesses.FirstOrDefaultAsync(c => c.PropertyId == id);

        var model = new CheckInProcessViewModel
        {
            PropertyId = property.Id,
            PropertyTitle = property.Title
        };

        if (existing != null)
        {
            model.Title = existing.Title;
            model.RequiredDocuments = existing.RequiredDocuments;
            model.StepsText = string.Join(Environment.NewLine, ReadSteps(existing.StepsJson));
        }
        else
        {
            // A sensible starting point the host can edit.
            model.Title = $"Check-in for {property.Title}";
            model.StepsText = string.Join(Environment.NewLine, new[]
            {
                "Confirm your arrival time",
                "Upload your ID or passport",
                "Read and accept the house rules"
            });
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(CheckInProcessViewModel model)
    {
        var userId = _userManager.GetUserId(User)!;

        var property = await _db.HostProperties
            .FirstOrDefaultAsync(p => p.Id == model.PropertyId && p.HostId == userId);
        if (property == null)
        {
            return NotFound();
        }

        model.PropertyTitle = property.Title;

        // One step per line, ignoring empty lines.
        var steps = (model.StepsText ?? string.Empty)
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (steps.Count == 0)
        {
            ModelState.AddModelError(nameof(model.StepsText), "Add at least one step.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var process = await _db.CheckInProcesses.FirstOrDefaultAsync(c => c.PropertyId == property.Id);
        if (process == null)
        {
            process = new CheckInProcess { PropertyId = property.Id };
            _db.CheckInProcesses.Add(process);
        }

        process.Title = model.Title.Trim();
        process.RequiredDocuments = model.RequiredDocuments.Trim();
        process.StepsJson = JsonSerializer.Serialize(steps);

        await _db.SaveChangesAsync();

        await _activityLogger.LogAsync(userId, ActivityType.PropertyUpdate,
            $"Updated check-in rules for '{property.Title}'");

        TempData["Success"] = "Check-in rules saved.";
        return RedirectToAction(nameof(Properties));
    }

    /// <summary>Turns the JSON text from the database back into a list of steps.</summary>
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
