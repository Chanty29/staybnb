using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models;

public class PropertyImage
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public HostProperty Property { get; set; } = null!;

    /// <summary>Web path of the saved file, e.g. /uploads/properties/abc.jpg</summary>
    [Required]
    public string ImageUrl { get; set; } = string.Empty;
}
