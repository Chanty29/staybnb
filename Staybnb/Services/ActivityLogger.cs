using Staybnb.Data;
using Staybnb.Models;
using Staybnb.Models.Enums;

namespace Staybnb.Services;

public class ActivityLogger : IActivityLogger
{
    private readonly ApplicationDbContext _db;

    public ActivityLogger(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(string? userId, ActivityType type, string action)
    {
        // The Action column is 100 characters, so cut anything longer.
        if (action.Length > 100)
        {
            action = action[..100];
        }

        _db.ActivityLogs.Add(new ActivityLog
        {
            UserId = userId,
            ActivityType = type,
            Action = action,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }
}
