namespace Staybnb.Models.Enums;

/// <summary>
/// Life of a booking. The main path is Pending -> Approved -> CheckedIn -> Completed.
/// </summary>
public enum BookingStatus
{
    Pending = 0,     // guest requested, waiting for the host
    Approved = 1,    // host approved
    Rejected = 2,    // host rejected
    Cancelled = 3,   // guest cancelled
    CheckedIn = 4,   // guest finished check-in and the host verified the documents
    Completed = 5    // stay is over
}
