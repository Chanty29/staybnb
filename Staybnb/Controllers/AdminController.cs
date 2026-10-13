using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Staybnb.Constants;

namespace Staybnb.Controllers;

// Admins and the SuperAdmin can open this. Other roles get "Access denied".
[Authorize(Roles = AppRoles.Admin + "," + AppRoles.SuperAdmin)]
public class AdminController : Controller
{
    public IActionResult Dashboard() => View();
}
