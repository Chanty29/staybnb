using Staybnb.Models.Enums;

namespace Staybnb.Services;

/// <summary>Writes rows to the ActivityLog table (the admin audit trail).</summary>
public interface IActivityLogger
{
    /// <param name="userId">The user the event is about. Null for events with no known user (e.g. a failed login).</param>
    Task LogAsync(string? userId, ActivityType type, string action);
}
