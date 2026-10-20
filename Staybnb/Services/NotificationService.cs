using Microsoft.AspNetCore.Identity;
using Staybnb.Constants;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.Models.Enums;

namespace Staybnb.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task NotifyAsync(string userId, string title, string message, NotificationType type)
    {
        _db.Notifications.Add(Build(userId, title, message, type));
        await _db.SaveChangesAsync();
    }

    public async Task NotifyAdminsAsync(string title, string message, NotificationType type)
    {
        var admins = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);
        var superAdmins = await _userManager.GetUsersInRoleAsync(AppRoles.SuperAdmin);

        // Distinct, in case someone is in both roles.
        var adminIds = admins.Concat(superAdmins).Select(u => u.Id).Distinct().ToList();

        foreach (var adminId in adminIds)
        {
            _db.Notifications.Add(Build(adminId, title, message, type));
        }

        if (adminIds.Count > 0)
        {
            await _db.SaveChangesAsync();
        }
    }

    private static Notification Build(string userId, string title, string message, NotificationType type)
    {
        return new Notification
        {
            UserId = userId,
            Title = Truncate(title, 100),
            Message = Truncate(message, 500),
            Type = type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
