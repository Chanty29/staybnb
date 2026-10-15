namespace Staybnb.Models.Enums;

/// <summary>Progress of a guest's check-in for one booking.</summary>
public enum CheckInStatus
{
    InProgress = 0,   // guest started but has not submitted
    Submitted = 1,    // guest submitted, waiting for host to verify documents
    Verified = 2,     // host verified, guest is checked in
    Rejected = 3      // host rejected a document, guest must re-upload
}
