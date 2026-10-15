using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models;

/// <summary>Wifi, Kitchen, Pool... Many-to-many with HostProperty.</summary>
public class Amenity
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Icon CSS class, e.g. "bi bi-wifi" (Bootstrap Icons).</summary>
    [StringLength(50)]
    public string IconClass { get; set; } = string.Empty;

    public ICollection<HostProperty> Properties { get; set; } = new List<HostProperty>();
}
