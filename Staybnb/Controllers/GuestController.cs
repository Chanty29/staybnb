using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Staybnb.Constants;

namespace Staybnb.Controllers;

// Guest-only area. A Host (or Admin) who tries to open this gets "Access denied".
[Authorize(Roles = AppRoles.Guest)]
public class GuestController : Controller
{
    public IActionResult Dashboard() => View();
}
