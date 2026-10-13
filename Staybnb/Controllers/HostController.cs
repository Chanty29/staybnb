using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Staybnb.Constants;

namespace Staybnb.Controllers;

// Host-only area. Guests cannot open this until an Admin approves their application.
[Authorize(Roles = AppRoles.Host)]
public class HostController : Controller
{
    public IActionResult Dashboard() => View();
}
