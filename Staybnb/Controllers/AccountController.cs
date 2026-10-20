using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Staybnb.Constants;
using Staybnb.Models;
using Staybnb.Models.Enums;
using Staybnb.Services;
using Staybnb.ViewModels;

namespace Staybnb.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IActivityLogger _activityLogger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IActivityLogger activityLogger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _activityLogger = activityLogger;
    }

    // ---------- Register ----------

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        // Every new registrant starts as a Guest.
        await _userManager.AddToRoleAsync(user, AppRoles.Guest);

        await _activityLogger.LogAsync(user.Id, ActivityType.Registration,
            $"New user registered: {user.Email}");

        await _signInManager.SignInAsync(user, isPersistent: false);
        return await RedirectByRoleAsync(user);
    }

    // ---------- Login ----------

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            await _activityLogger.LogAsync(null, ActivityType.Login,
                $"Failed login attempt for {model.Email}");

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        await _activityLogger.LogAsync(user!.Id, ActivityType.Login, $"User logged in: {user.Email}");

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return await RedirectByRoleAsync(user);
    }

    // ---------- Logout ----------

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = _userManager.GetUserId(User);

        await _signInManager.SignOutAsync();

        if (userId != null)
        {
            await _activityLogger.LogAsync(userId, ActivityType.Logout, "User logged out");
        }

        return RedirectToAction("Index", "Home");
    }

    // ---------- Access denied ----------

    [HttpGet]
    public IActionResult AccessDenied() => View();

    // ---------- Helper ----------

    /// <summary>
    /// Sends each role to its own dashboard after login/registration.
    /// </summary>
    private async Task<IActionResult> RedirectByRoleAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains(AppRoles.SuperAdmin) || roles.Contains(AppRoles.Admin))
        {
            return RedirectToAction("Dashboard", "Admin");
        }

        if (roles.Contains(AppRoles.Host))
        {
            return RedirectToAction("Dashboard", "Host");
        }

        return RedirectToAction("Dashboard", "Guest");
    }
}
