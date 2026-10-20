using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Constants;
using Staybnb.Data;
using Staybnb.Models;

namespace Staybnb.Controllers;

// Guest-only area. A Host (or Admin) who tries to open this gets "Access denied".
[Authorize(Roles = AppRoles.Guest)]
public class GuestController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public GuestController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Dashboard()
    {
        var userId = _userManager.GetUserId(User)!;

        // The guest's most recent host application (null if they never applied).
        var latestApplication = await _db.HostApplications
            .Include(a => a.Property)
            .Where(a => a.ApplicationUserId == userId)
            .OrderByDescending(a => a.AppliedAt)
            .FirstOrDefaultAsync();

        return View(latestApplication);
    }
}
