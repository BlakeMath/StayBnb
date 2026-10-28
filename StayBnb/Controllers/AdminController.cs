using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnb.Data;
using StayBnb.Models;
using StayBnb.ViewModels;

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewHostApplication(
            int id, string decision)
        {
            if (decision != "Approve" && decision != "Reject")
            {
                return BadRequest();
            }

            // Keep the decision and its related changes together.
            await using var transaction =await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var application = await _context.HostApplications.FindAsync(id);

            if (application == null)
            {
                return NotFound();
            }

            if (application.Status != ApplicationStatus.Pending)
            {
                TempData["Error"] = "This application has already been reviewed.";
                return RedirectToAction(nameof(HostApplications));
            }

            var user = await _userManager.FindByIdAsync(application.ApplicationUserId);

            if (user == null)
            {
                return NotFound();
            }

            if (decision == "Approve")
            {
                var property = await _context.HostProperties.FindAsync(application.PropertyId);

                var hasImage = await _context.PropertyImages.AnyAsync(image => image.PropertyId == application.PropertyId);

                if (property == null || property.HostId != user.Id || !hasImage)
                {
                    TempData["Error"] = "Approval requires a matching property and at least one image.";

                    return RedirectToAction(nameof(HostApplications));
                }

                var isGuest = await _userManager.IsInRoleAsync(user, "Guest");
                var isHost = await _userManager.IsInRoleAsync(user, "Host");

                if (!isGuest && !isHost)
                {
                    TempData["Error"] = "The applicant must currently be a Guest or Host.";

                    return RedirectToAction(nameof(HostApplications));
                }

                if (!isHost)
                {
                    var addResult = await _userManager.AddToRoleAsync(user, "Host");

                    if (!addResult.Succeeded)
                    {
                        TempData["Error"] = string.Join(" ", addResult.Errors.Select(error => error.Description));

                        return RedirectToAction(nameof(HostApplications));
                    }
                }

                if (isGuest)
                {
                    var removeResult = await _userManager.RemoveFromRoleAsync(user, "Guest");

                    if (!removeResult.Succeeded)
                    {
                        TempData["Error"] = string.Join(" ", removeResult.Errors.Select(error => error.Description));

                        return RedirectToAction(nameof(HostApplications));
                    }
                }

                if (!isHost || isGuest)
                {
                    // Existing login sessions must refresh after the role change.
                    var stampResult = await _userManager.UpdateSecurityStampAsync(user);

                    if (!stampResult.Succeeded)
                    {
                        TempData["Error"] = string.Join(" ", stampResult.Errors.Select(error => error.Description));

                        return RedirectToAction(nameof(HostApplications));
                    }

                    _context.ActivityLogs.Add(new ActivityLog
                    {
                        UserId = _userManager.GetUserId(User)!,
                        Action = $"Set user {user.Id} to Host; Guest role removed",
                        ActivityType = ActivityType.RoleChanged,
                        CreatedAt = DateTime.Now
                    });
                }

                property.IsActive = true;
                application.Status = ApplicationStatus.Approved;
            }
            else
            {
                application.Status = ApplicationStatus.Rejected;
            }

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = _userManager.GetUserId(User)!,
                Action = $"Host application {application.Id}: {application.Status}",
                ActivityType = ActivityType.HostApplicationReviewed,
                CreatedAt = DateTime.Now
            });

            _context.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Title = $"Host application {application.Id}: {application.Status}",
                Type = NotificationType.HostApplicationUpdate,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] =
                $"Application {application.Id} was {application.Status.ToString().ToLowerInvariant()}.";

            return RedirectToAction(nameof(HostApplications));
        }

        public async Task<IActionResult> HostApplications()
        {
            var applications = await (
                from application in _context.HostApplications
                join property in _context.HostProperties
                    on application.PropertyId equals property.Id
                join user in _context.Users
                    on application.ApplicationUserId equals user.Id
                where application.Status == ApplicationStatus.Pending
                orderby application.AppliedAt, application.Id
                select new HostApplicationListViewModel
                {
                    ApplicationId = application.Id,
                    PropertyTitle = property.Title,
                    ApplicantEmail = user.Email ?? string.Empty,
                    AppliedAt = application.AppliedAt,
                    Description = property.Description,
                    Address = property.Address,
                    PricePerNight = property.PricePerNight,
                    ImageUrls = _context.PropertyImages
                    .Where(image => image.PropertyId == property.Id)
                    .OrderBy(image => image.Id)
                    .Select(image => image.ImageUrl)
                    .ToList()
                })
                .ToListAsync();

            return View(applications);
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