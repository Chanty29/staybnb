using Staybnb.Models;

namespace Staybnb.ViewModels;

/// <summary>One row in the inbox: the other person, the latest message and how many are unread.</summary>
public class InboxItemViewModel
{
    public ApplicationUser Other { get; set; } = null!;
    public Message LastMessage { get; set; } = null!;
    public int Unread { get; set; }
}

/// <summary>One conversation thread between the signed-in user and one other user.</summary>
public class ConversationViewModel
{
    public ApplicationUser Other { get; set; } = null!;
    public string CurrentUserId { get; set; } = string.Empty;
    public List<Message> Messages { get; set; } = new();
}
