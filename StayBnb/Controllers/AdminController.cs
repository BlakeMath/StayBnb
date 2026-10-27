using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnb.Data;
using StayBnb.Models;

namespace StayBnb.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AdminController(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> ActivityLogs()
        {
            var logs = await _context.ActivityLogs
                .AsNoTracking()
                .OrderByDescending(log => log.CreatedAt)
                .ThenByDescending(log => log.Id)
                .ToListAsync();

            return View(logs);
        }

        // GET: Admin/Users
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Users(string? search)
        {
            var guests = await _userManager.GetUsersInRoleAsync("Guest");
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");

            // Do not offer promotion to existing administrators.
            var adminIds = admins.Concat(superAdmins).Select(user => user.Id).ToHashSet();

            var users = guests.Where(user => !adminIds.Contains(user.Id));

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                users = users.Where(user => user.Email != null && user.Email.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            ViewData["Search"] = search;

            return View(users.OrderBy(user => user.Email).ToList());
        }

        // GET: Admin/PromoteToAdmin
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> PromoteToAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // Check eligibility again when the form is submitted.
            if (!await _userManager.IsInRoleAsync(user, "Guest") || await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                TempData["Error"] = "Only Guests who are not already administrators can be promoted.";

                return RedirectToAction(nameof(Users));
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var result = await _userManager.AddToRoleAsync(user, "Admin");

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" ", result.Errors.Select(error => error.Description));

                return RedirectToAction(nameof(Users));
            }

            // Record who performed the promotion and the target account.
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = _userManager.GetUserId(User)!,
                Action = $"Promoted user {user.Id} to Admin",
                ActivityType = ActivityType.RoleChanged,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = "The Guest was promoted to Admin.";

            return RedirectToAction(nameof(Users));
        }
    }
}