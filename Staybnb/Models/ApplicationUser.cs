using Microsoft.AspNetCore.Identity;

namespace Staybnb.Models;

/// <summary>
/// Our user. Extends IdentityUser (which already has Id, Email, PasswordHash, etc.)
/// with the extra fields from the entity diagram.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string FullName => $"{FirstName} {LastName}".Trim();
}
