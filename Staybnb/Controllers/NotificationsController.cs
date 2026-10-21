using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;

namespace Staybnb.Controllers;

// Every signed-in user (any role) has their own notifications. They only ever see their own.
[Authorize]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // filter: "all" (default), "unread" or "read"
    public async Task<IActionResult> Index(string? filter)
    {
        var userId = _userManager.GetUserId(User)!;
        var query = _db.Notifications.Where(n => n.UserId == userId);

        filter = (filter ?? "all").ToLowerInvariant();
        if (filter == "unread")
        {
            query = query.Where(n => !n.IsRead);
        }
        else if (filter == "read")
        {
            query = query.Where(n => n.IsRead);
        }
        else
        {
            filter = "all";
        }

        ViewData["Filter"] = filter;

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .ToListAsync();

        return View(items);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id, string? filter)
    {
        var userId = _userManager.GetUserId(User)!;

        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (notification == null)
        {
            return NotFound();
        }

        notification.IsRead = true;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { filter });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = _userManager.GetUserId(User)!;

        var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        foreach (var n in unread)
        {
            n.IsRead = true;
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = "All notifications marked as read.";
        return RedirectToAction(nameof(Index));
    }
}
