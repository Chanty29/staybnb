using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.Models.Enums;
using Staybnb.Services;
using Staybnb.ViewModels;

namespace Staybnb.Controllers;

// Database-backed inbox (no SignalR). Any signed-in user can message another user.
[Authorize]
public class MessagesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogger _activityLogger;
    private readonly INotificationService _notifications;

    public MessagesController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IActivityLogger activityLogger,
        INotificationService notifications)
    {
        _db = db;
        _userManager = userManager;
        _activityLogger = activityLogger;
        _notifications = notifications;
    }

    // ---------- Inbox: one row per person I have talked to ----------

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;

        var messages = await _db.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync();

        var items = messages
            .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Select(g =>
            {
                var last = g.First();
                var other = last.SenderId == userId ? last.Receiver : last.Sender;
                return new InboxItemViewModel
                {
                    Other = other,
                    LastMessage = last,
                    Unread = g.Count(m => m.ReceiverId == userId && !m.IsRead)
                };
            })
            .OrderByDescending(i => i.LastMessage.Timestamp)
            .ToList();

        return View(items);
    }

    // ---------- One conversation (id = the other user's id) ----------

    [HttpGet]
    public async Task<IActionResult> Conversation(string id)
    {
        var userId = _userManager.GetUserId(User)!;

        if (id == userId)
        {
            return RedirectToAction(nameof(Index));
        }

        var other = await _userManager.FindByIdAsync(id);
        if (other == null)
        {
            return NotFound();
        }

        var messages = await _db.Messages
            .Where(m => (m.SenderId == userId && m.ReceiverId == id) || (m.SenderId == id && m.ReceiverId == userId))
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        // Opening the thread marks what the other person sent me as read.
        var unread = messages.Where(m => m.ReceiverId == userId && !m.IsRead).ToList();
        if (unread.Count > 0)
        {
            foreach (var m in unread)
            {
                m.IsRead = true;
            }
            await _db.SaveChangesAsync();
        }

        return View(new ConversationViewModel
        {
            Other = other,
            CurrentUserId = userId,
            Messages = messages
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(string receiverId, string? content)
    {
        var userId = _userManager.GetUserId(User)!;

        if (receiverId == userId)
        {
            return RedirectToAction(nameof(Index));
        }

        var receiver = await _userManager.FindByIdAsync(receiverId);
        if (receiver == null)
        {
            return NotFound();
        }

        content = content?.Trim();
        if (string.IsNullOrEmpty(content) || content.Length > 2000)
        {
            TempData["Error"] = "Write a message of up to 2000 characters.";
            return RedirectToAction(nameof(Conversation), new { id = receiverId });
        }

        _db.Messages.Add(new Message
        {
            SenderId = userId,
            ReceiverId = receiverId,
            Content = content,
            Timestamp = DateTime.UtcNow,
            IsRead = false
        });
        await _db.SaveChangesAsync();

        var sender = await _userManager.GetUserAsync(User);

        await _notifications.NotifyAsync(receiverId,
            "New Message",
            $"You have a new message from {sender?.FullName ?? "a user"}.",
            NotificationType.NewMessage);

        await _activityLogger.LogAsync(userId, ActivityType.Message, $"Message sent to {receiver.Email}");

        return RedirectToAction(nameof(Conversation), new { id = receiverId });
    }
}
