namespace Staybnb.ViewModels;

/// <summary>One row in the SuperAdmin "Users" table.</summary>
public class UserRowViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }

    /// <summary>Only Guests can be promoted to Admin.</summary>
    public bool IsGuest { get; set; }
}
