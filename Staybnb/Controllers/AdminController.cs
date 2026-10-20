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

// Admins and the SuperAdmin can open this. Other roles get "Access denied".
[Authorize(Roles = AppRoles.Admin + "," + AppRoles.SuperAdmin)]
public class AdminController : Controller
{
    private const int LogPageSize = 25;

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogger _activityLogger;
    private readonly INotificationService _notifications;

    public AdminController(
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
        var model = new AdminDashboardViewModel
        {
            TotalUsers = await _userManager.Users.CountAsync(),
            PendingApplications = await _db.HostApplications.CountAsync(a => a.Status == ApplicationStatus.Pending),
            ActiveProperties = await _db.HostProperties.CountAsync(p => p.IsActive),
            TotalBookings = await _db.Bookings.CountAsync(),
            ApprovedHosts = (await _userManager.GetUsersInRoleAsync(AppRoles.Host)).Count,

            RecentActivity = await _db.ActivityLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt)
                .Take(8)
                .ToListAsync(),

            RecentApplications = await _db.HostApplications
                .Include(a => a.ApplicationUser)
                .Include(a => a.Property)
                .OrderByDescending(a => a.AppliedAt)
                .Take(5)
                .ToListAsync()
        };

        return View(model);
    }

    // ---------- Host applications (approve / reject) ----------

    public async Task<IActionResult> Applications()
    {
        var applications = await _db.HostApplications
            .Include(a => a.ApplicationUser)
            .Include(a => a.Property)
                .ThenInclude(p => p.Images)
            .OrderBy(a => a.Status)               // Pending (0) comes first
            .ThenByDescending(a => a.AppliedAt)
            .ToListAsync();

        return View(applications);
    }

    /// <summary>
    /// Approving does four things: marks the application approved, activates the property,
    /// ADDS the Host role and REMOVES the Guest role (a user is a Guest or a Host, never both).
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveApplication(int id)
    {
        var application = await _db.HostApplications
            .Include(a => a.ApplicationUser)
            .Include(a => a.Property)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application == null)
        {
            return NotFound();
        }

        if (application.Status != ApplicationStatus.Pending)
        {
            TempData["Error"] = "This application has already been reviewed.";
            return RedirectToAction(nameof(Applications));
        }

        var admin = await _userManager.GetUserAsync(User);
        var applicant = application.ApplicationUser;
        var title = application.Property.Title;

        // 1. Role swap: add Host first, then remove Guest.
        if (!await _userManager.IsInRoleAsync(applicant, AppRoles.Host))
        {
            var addResult = await _userManager.AddToRoleAsync(applicant, AppRoles.Host);
            if (!addResult.Succeeded)
            {
                TempData["Error"] = "Could not give the user the Host role: " +
                    string.Join("; ", addResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Applications));
            }
        }

        if (await _userManager.IsInRoleAsync(applicant, AppRoles.Guest))
        {
            var removeResult = await _userManager.RemoveFromRoleAsync(applicant, AppRoles.Guest);
            if (!removeResult.Succeeded)
            {
                TempData["Error"] = "Could not remove the Guest role: " +
                    string.Join("; ", removeResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Applications));
            }
        }

        // 2. Approve the application and put the property live.
        application.Status = ApplicationStatus.Approved;
        application.ReviewedAt = DateTime.UtcNow;
        application.ReviewedById = admin?.Id;
        application.Property.IsActive = true;
        await _db.SaveChangesAsync();

        // 3. Audit trail and notification.
        await _activityLogger.LogAsync(admin?.Id, ActivityType.RoleChange,
            $"Role changed Guest -> Host for {applicant.Email}");
        await _activityLogger.LogAsync(admin?.Id, ActivityType.HostApplication,
            $"Approved host application for '{title}'");

        await _notifications.NotifyAsync(applicant.Id,
            "Host Application Approved",
            $"Congratulations! Your host application for '{title}' has been approved. You are now a Host.",
            NotificationType.HostApplicationApproved);

        TempData["Success"] = $"Approved '{title}'. {applicant.FullName} is now a Host.";
        return RedirectToAction(nameof(Applications));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectApplication(int id, string? reason)
    {
        var application = await _db.HostApplications
            .Include(a => a.ApplicationUser)
            .Include(a => a.Property)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application == null)
        {
            return NotFound();
        }

        if (application.Status != ApplicationStatus.Pending)
        {
            TempData["Error"] = "This application has already been reviewed.";
            return RedirectToAction(nameof(Applications));
        }

        reason = reason?.Trim();
        if (string.IsNullOrEmpty(reason))
        {
            TempData["Error"] = "Please type a reason for rejecting the application.";
            return RedirectToAction(nameof(Applications));
        }

        if (reason.Length > 500)
        {
            reason = reason[..500];
        }

        var admin = await _userManager.GetUserAsync(User);
        var title = application.Property.Title;

        application.Status = ApplicationStatus.Rejected;
        application.ReviewedAt = DateTime.UtcNow;
        application.ReviewedById = admin?.Id;
        application.RejectionReason = reason;
        application.Property.IsActive = false;
        await _db.SaveChangesAsync();

        await _activityLogger.LogAsync(admin?.Id, ActivityType.HostApplication,
            $"Rejected host application for '{title}'");

        await _notifications.NotifyAsync(application.ApplicationUserId,
            "Host Application Rejected",
            $"Your host application for '{title}' was rejected. Reason: {reason}",
            NotificationType.HostApplicationRejected);

        TempData["Success"] = $"Rejected '{title}'.";
        return RedirectToAction(nameof(Applications));
    }

    // ---------- Users (SuperAdmin only) ----------

    [Authorize(Roles = AppRoles.SuperAdmin)]
    public async Task<IActionResult> Users(string? q)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(u =>
                u.Email!.Contains(term) || u.FirstName.Contains(term) || u.LastName.Contains(term));
        }

        var users = await query.OrderBy(u => u.Email).Take(200).ToListAsync();
        var ids = users.Select(u => u.Id).ToList();

        // Look up the role names for these users in one query.
        var roleRows = await (
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            where ids.Contains(ur.UserId)
            select new { ur.UserId, RoleName = r.Name }).ToListAsync();

        var rows = users.Select(u =>
        {
            var roles = roleRows
                .Where(x => x.UserId == u.Id && x.RoleName != null)
                .Select(x => x.RoleName!)
                .ToList();

            return new UserRowViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                CreatedAt = u.CreatedAt,
                Roles = roles,
                IsGuest = roles.Contains(AppRoles.Guest)
            };
        }).ToList();

        ViewData["Query"] = q;
        return View(rows);
    }

    /// <summary>Only the SuperAdmin can do this. The Guest becomes an Admin.</summary>
    [Authorize(Roles = AppRoles.SuperAdmin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteToAdmin(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (!await _userManager.IsInRoleAsync(user, AppRoles.Guest))
        {
            TempData["Error"] = "Only Guests can be promoted to Admin.";
            return RedirectToAction(nameof(Users));
        }

        var addResult = await _userManager.AddToRoleAsync(user, AppRoles.Admin);
        if (!addResult.Succeeded)
        {
            TempData["Error"] = "Could not promote the user: " +
                string.Join("; ", addResult.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Users));
        }

        await _userManager.RemoveFromRoleAsync(user, AppRoles.Guest);

        var superAdminId = _userManager.GetUserId(User);
        await _activityLogger.LogAsync(superAdminId, ActivityType.RoleChange,
            $"Role changed Guest -> Admin for {user.Email}");

        await _notifications.NotifyAsync(user.Id,
            "You are now an Admin",
            "The SuperAdmin promoted you to Admin. You can now review host applications.",
            NotificationType.RoleChanged);

        TempData["Success"] = $"{user.FullName} is now an Admin.";
        return RedirectToAction(nameof(Users));
    }

    // ---------- Activity log ----------

    public async Task<IActionResult> ActivityLogs(ActivityType? type, int page = 1)
    {
        var query = _db.ActivityLogs.Include(l => l.User).AsQueryable();

        if (type.HasValue)
        {
            query = query.Where(l => l.ActivityType == type.Value);
        }

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)LogPageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * LogPageSize)
            .Take(LogPageSize)
            .ToListAsync();

        return View(new ActivityLogsViewModel
        {
            Items = items,
            Filter = type,
            Page = page,
            TotalPages = totalPages,
            TotalItems = total
        });
    }
}
