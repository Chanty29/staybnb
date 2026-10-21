using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;

namespace Staybnb.Controllers;

// Public: anyone can look at an active listing. Only Guests can book it (see BookingsController).
public class PropertiesController : Controller
{
    private readonly ApplicationDbContext _db;

    public PropertiesController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Details(int id)
    {
        var property = await _db.HostProperties
            .Include(p => p.Images)
            .Include(p => p.Amenities)
            .Include(p => p.Host)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        if (property == null)
        {
            return NotFound();
        }

        return View(property);
    }
}
