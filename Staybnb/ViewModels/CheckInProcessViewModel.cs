using System.ComponentModel.DataAnnotations;

namespace Staybnb.ViewModels;

/// <summary>The form a host uses to set the check-in rules for one property.</summary>
public class CheckInProcessViewModel
{
    public int PropertyId { get; set; }

    /// <summary>Shown at the top of the page. Filled by the controller, not posted.</summary>
    public string PropertyTitle { get; set; } = string.Empty;

    [Required, StringLength(200)]
    [Display(Name = "Check-in title")]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(200)]
    [Display(Name = "Required documents")]
    public string RequiredDocuments { get; set; } = "ID or Passport";

    /// <summary>One step per line. The controller turns this into JSON for the database.</summary>
    [Required(ErrorMessage = "Add at least one step.")]
    [Display(Name = "Steps (one per line)")]
    public string StepsText { get; set; } = string.Empty;
}
