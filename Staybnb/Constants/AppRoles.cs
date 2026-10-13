namespace Staybnb.Constants;

/// <summary>
/// Central place for role names so we never mistype a role string.
/// Use these inside [Authorize(Roles = AppRoles.Host)] etc.
/// </summary>
public static class AppRoles
{
    public const string Guest = "Guest";
    public const string Host = "Host";
    public const string Admin = "Admin";
    public const string SuperAdmin = "SuperAdmin";

    public static readonly string[] All = { Guest, Host, Admin, SuperAdmin };
}
