namespace Staybnb.Models.Enums;

/// <summary>What kind of event an ActivityLog row records (shown in the admin audit screen).</summary>
public enum ActivityType
{
    Login = 0,
    Logout = 1,
    Registration = 2,
    RoleChange = 3,
    HostApplication = 4,
    PropertyUpdate = 5,
    BookingStatusChange = 6,
    CheckIn = 7,
    Message = 8,
    System = 9
}
