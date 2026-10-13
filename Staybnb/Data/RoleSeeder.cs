using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Staybnb.Constants;
using Staybnb.Models;

namespace Staybnb.Data;

/// <summary>
/// Runs once when the app starts. It:
///   1. applies any pending migrations (creates the database if needed),
///   2. creates the four roles (Guest, Host, Admin, SuperAdmin),
///   3. creates the SuperAdmin user from appsettings.json.
/// It is safe to run many times - it only creates what is missing.
/// </summary>
public static class RoleSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var config = services.GetRequiredService<IConfiguration>();

        // 1. Make sure the database exists and is up to date.
        await context.Database.MigrateAsync();

        // 2. Create roles.
        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // 3. Create the SuperAdmin user.
        var email = config["SuperAdmin:Email"];
        var password = config["SuperAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "SuperAdmin:Email and SuperAdmin:Password must be set in appsettings.json.");
        }

        var superAdmin = await userManager.FindByEmailAsync(email);
        if (superAdmin == null)
        {
            superAdmin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = config["SuperAdmin:FirstName"] ?? "Super",
                LastName = config["SuperAdmin:LastName"] ?? "Admin",
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(superAdmin, password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Could not create SuperAdmin: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(superAdmin, AppRoles.SuperAdmin))
        {
            await userManager.AddToRoleAsync(superAdmin, AppRoles.SuperAdmin);
        }
    }
}
