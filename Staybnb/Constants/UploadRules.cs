namespace Staybnb.Constants;

/// <summary>Rules for uploaded property photos. Kept in one place so the controller and views agree.</summary>
public static class UploadRules
{
    public static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    public const int MaxImages = 6;

    public const long MaxImageSizeBytes = 4 * 1024 * 1024; // 4 MB per photo
}
