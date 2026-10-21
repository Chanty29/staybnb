using Staybnb.Models;
using Staybnb.Models.Enums;

namespace Staybnb.ViewModels;

public class HomeIndexViewModel
{
    public List<HostProperty> Properties { get; set; } = new();
    public List<string> Cities { get; set; } = new();
    public string? City { get; set; }
    public PropertyType? Type { get; set; }
}
