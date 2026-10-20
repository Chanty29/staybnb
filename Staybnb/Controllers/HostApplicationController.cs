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

/// <summary>
/// The "Become a Host" flow. A Guest submits a property (with photos) as a HostApplication;
/// an Admin approves it later (phase 6). Existing Hosts can also use it to add another property.
/// </summary>
[Authorize(Roles = AppRoles.Guest + "," + AppRoles.Host)]
public class HostApplicationController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;
    private readonly IActivityLogger _activityLogger;
    private readonly INotificationService _notifications;

    public HostApplicationController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment env,
        IActivityLogger activityLogger,
        INotificationService notifications)
    {
        _db = db;
        _userManager = userManager;
        _env = env;
        _activityLogger = activityLogger;
        _notifications = notifications;
    }

    // ---------- The form ----------

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var userId = _userManager.GetUserId(User)!;

        if (await HasPendingApplicationAsync(userId))
        {
            TempData["Error"] = "You already have an application waiting for review. Please wait for an Admin to respond.";
            return RedirectToAction(nameof(Status));
        }

        var model = new HostApplicationViewModel();
        await LoadAmenitiesAsync(model);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HostApplicationViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        if (await HasPendingApplicationAsync(user.Id))
        {
            TempData["Error"] = "You already have an application waiting for review. Please wait for an Admin to respond.";
            return RedirectToAction(nameof(Status));
        }

        // An application is incomplete (and cannot be submitted) without at least one image.
        var files = (model.Images ?? new List<IFormFile>())
            .Where(f => f != null && f.Length > 0)
            .ToList();
        ValidateImages(files);

        if (!ModelState.IsValid)
        {
            await LoadAmenitiesAsync(model);
            return View(model);
        }

        var title = model.Title.Trim();
        var savedFiles = new List<string>(); // web paths of the photos we saved

        try
        {
            // 1. Save the photo files into wwwroot/uploads/properties
            var folder = Path.Combine(_env.WebRootPath, "uploads", "properties");
            Directory.CreateDirectory(folder);

            foreach (var file in files)
            {
                // Never trust the user's file name: make our own.
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var fileName = $"{Guid.NewGuid():N}{extension}";
                var fullPath = Path.Combine(folder, fileName);

                await using (var stream = System.IO.File.Create(fullPath))
                {
                    await file.CopyToAsync(stream);
                }

                savedFiles.Add($"/uploads/properties/{fileName}");
            }

            // 2. Build the property (inactive until an Admin approves it), its images and the application
            var amenities = await _db.Amenities
                .Where(a => model.SelectedAmenityIds.Contains(a.Id))
                .ToListAsync();

            var property = new HostProperty
            {
                Title = title,
                Description = model.Description.Trim(),
                Address = model.Address.Trim(),
                City = model.City.Trim(),
                Country = model.Country.Trim(),
                PropertyType = model.PropertyType,
                PricePerNight = model.PricePerNight,
                CleaningFee = model.CleaningFee,
                MaxGuests = model.MaxGuests,
                Bedrooms = model.Bedrooms,
                Beds = model.Beds,
                Bathrooms = model.Bathrooms,
                IsActive = false,
                HostId = user.Id,
                Amenities = amenities,
                Images = savedFiles.Select(url => new PropertyImage { ImageUrl = url }).ToList()
            };

            var application = new HostApplication
            {
                ApplicationUserId = user.Id,
                Property = property,
                Status = ApplicationStatus.Pending,
                AppliedAt = DateTime.UtcNow
            };

            // 3. One SaveChanges saves the property, images and application together (all or nothing)
            _db.HostApplications.Add(application);
            await _db.SaveChangesAsync();
        }
        catch (Exception)
        {
            // Don't leave orphan photo files behind if the database save failed.
            DeleteFiles(savedFiles);

            ModelState.AddModelError(string.Empty, "Something went wrong while submitting your application. Please try again.");
            await LoadAmenitiesAsync(model);
            return View(model);
        }

        await _activityLogger.LogAsync(user.Id, ActivityType.HostApplication,
            $"Submitted host application for '{title}'");

        await _notifications.NotifyAsync(user.Id,
            "Host Application Submitted",
            $"Your application for '{title}' has been received and is under review.",
            NotificationType.HostApplicationSubmitted);

        await _notifications.NotifyAdminsAsync(
            "New Host Application",
            $"{user.FullName} applied to host '{title}'.",
            NotificationType.HostApplicationSubmitted);

        TempData["Success"] = "Your application was submitted. An Admin will review it soon.";
        return RedirectToAction(nameof(Status));
    }

    // ---------- Status page ----------

    [HttpGet]
    public async Task<IActionResult> Status()
    {
        var userId = _userManager.GetUserId(User)!;

        var applications = await _db.HostApplications
            .Include(a => a.Property)
                .ThenInclude(p => p.Images)
            .Where(a => a.ApplicationUserId == userId)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync();

        return View(applications);
    }

    // ---------- Helpers ----------

    /// <summary>A Guest may only have one application waiting at a time. Hosts adding more properties are not limited.</summary>
    private async Task<bool> HasPendingApplicationAsync(string userId)
    {
        if (!User.IsInRole(AppRoles.Guest))
        {
            return false;
        }

        return await _db.HostApplications.AnyAsync(a =>
            a.ApplicationUserId == userId && a.Status == ApplicationStatus.Pending);
    }

    private async Task LoadAmenitiesAsync(HostApplicationViewModel model)
    {
        model.AllAmenities = await _db.Amenities.OrderBy(a => a.Name).ToListAsync();
    }

    private void ValidateImages(List<IFormFile> files)
    {
        const string key = nameof(HostApplicationViewModel.Images);

        if (files.Count == 0)
        {
            ModelState.AddModelError(key, "Please add at least one photo. An application cannot be submitted without an image.");
            return;
        }

        if (files.Count > UploadRules.MaxImages)
        {
            ModelState.AddModelError(key, $"You can upload at most {UploadRules.MaxImages} photos.");
        }

        foreach (var file in files)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!UploadRules.AllowedImageExtensions.Contains(extension) ||
                !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(key, $"'{file.FileName}' is not a supported image. Use JPG, PNG or WEBP.");
            }
            else if (file.Length > UploadRules.MaxImageSizeBytes)
            {
                ModelState.AddModelError(key, $"'{file.FileName}' is too big. Each photo must be 4 MB or smaller.");
            }
        }
    }

    private void DeleteFiles(IEnumerable<string> webPaths)
    {
        foreach (var webPath in webPaths)
        {
            try
            {
                var relative = webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(_env.WebRootPath, relative);

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch (IOException)
            {
                // Cleanup is best-effort: nothing more we can do.
            }
        }
    }
}
