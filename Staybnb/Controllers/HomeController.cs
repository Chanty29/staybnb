using Microsoft.AspNetCore.Mvc;

namespace Staybnb.Controllers;

public class HomeController : Controller
{
    // Public landing page. Phase 5 turns this into the property gallery.
    public IActionResult Index() => View();

    // Used by the production exception handler in Program.cs.
    public IActionResult Error() => Content("Something went wrong. Please try again.");
}
