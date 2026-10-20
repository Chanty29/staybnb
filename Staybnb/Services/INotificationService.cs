using Staybnb.Models.Enums;

namespace Staybnb.Services;

/// <summary>Creates notifications (the bell icon) for one user or for all admins.</summary>
public interface INotificationService
{
    Task NotifyAsync(string userId, string title, string message, NotificationType type);

    /// <summary>Sends the same notification to every Admin and the SuperAdmin.</summary>
    Task NotifyAdminsAsync(string title, string message, NotificationType type);
}
