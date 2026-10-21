using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models.Enums;
using Staybnb.ViewModels;

namespace Staybnb.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db)
    {
        _db = db;
    }

    // Public gallery: only ACTIVE properties, optionally filtered by city and/or type.
    public async Task<IActionResult> Index(string? city, PropertyType? type)
    {
        var active = _db.HostProperties.Where(p => p.IsActive);

        var cities = await active
            .Select(p => p.City)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        var query = active.Include(p => p.Images).AsQueryable();

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(p => p.City == city);
        }

        if (type.HasValue)
        {
            query = query.Where(p => p.PropertyType == type.Value);
        }

        var model = new HomeIndexViewModel
        {
            Properties = await query.OrderByDescending(p => p.CreatedAt).ToListAsync(),
            Cities = cities,
            City = city,
            Type = type
        };

        return View(model);
    }

    // Used by the production exception handler in Program.cs.
    public IActionResult Error() => Content("Something went wrong. Please try again.");
}
