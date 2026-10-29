using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnb.Data;
using StayBnb.ViewModels;
using System.Security.Claims;
using StayBnb.Models;

namespace StayBnb.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MessagesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var messages = await (
                from message in _context.Messages
                join sender in _context.Users
                    on message.SenderId equals sender.Id
                where message.ReceiverId == userId
                orderby message.Timestamp descending, message.Id descending
                select new InboxMessageViewModel
                {
                    SenderEmail = sender.Email ?? string.Empty,
                    Content = message.Content,
                    Timestamp = message.Timestamp
                })
                .ToListAsync();

            return View(messages);
        }

        [HttpGet]
        public IActionResult Send()
        {
            return View(new SendMessageViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(SendMessageViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.RecipientEmail.Trim().ToUpperInvariant();

            var recipient = await _context.Users.FirstOrDefaultAsync(user => user.NormalizedEmail == email);

            if (recipient == null || !await _context.ApplicationUsers.AnyAsync(profile => profile.Id == recipient.Id))
            {
                ModelState.AddModelError(nameof(model.RecipientEmail), "No StayBnb account was found for that email.");

                return View(model);
            }

            if (recipient.Id == userId)
            {
                ModelState.AddModelError(nameof(model.RecipientEmail), "Please choose another user.");

                return View(model);
            }

            _context.Messages.Add(new Message
            {
                SenderId = userId,
                ReceiverId = recipient.Id,
                Content = model.Content.Trim(),
                Timestamp = DateTime.Now
            });

            _context.Notifications.Add(new Notification
            {
                UserId = recipient.Id,
                Title = "You have received a new message.",
                Type = NotificationType.NewMessage,
                CreatedAt = DateTime.Now
            });

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId,
                Action = "Sent a message.",
                ActivityType = ActivityType.MessageSent,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Your message has been sent.";

            return RedirectToAction(nameof(Index));
        }
    }
}