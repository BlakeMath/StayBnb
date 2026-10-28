using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnb.Data;
using StayBnb.Models;
using StayBnb.ViewModels;

namespace StayBnb.Controllers
{
    [Authorize(Roles = "Host")]
    public class HostController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HostController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var properties = await _context.HostProperties
                .AsNoTracking()
                .Where(property =>
                    property.HostId == userId &&
                    _context.HostApplications.Any(application =>
                        application.PropertyId == property.Id &&
                        application.ApplicationUserId == userId &&
                        application.Status == ApplicationStatus.Approved))
                .OrderBy(property => property.Title)
                .ThenBy(property => property.Id)
                .ToListAsync();

            return View(properties);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var property = await _context.HostProperties
                .AsNoTracking()
                .FirstOrDefaultAsync(property =>
                    property.Id == id &&
                    property.HostId == userId &&
                    _context.HostApplications.Any(application =>
                        application.PropertyId == property.Id &&
                        application.ApplicationUserId == userId &&
                        application.Status == ApplicationStatus.Approved));

            if (property == null)
            {
                return NotFound();
            }

            var model = new EditPropertyViewModel
            {
                Id = property.Id,
                Title = property.Title,
                Description = property.Description,
                Address = property.Address,
                PricePerNight = property.PricePerNight
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditPropertyViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var property = await _context.HostProperties
                .FirstOrDefaultAsync(property =>
                    property.Id == model.Id &&
                    property.HostId == userId &&
                    _context.HostApplications.Any(application =>
                        application.PropertyId == property.Id &&
                        application.ApplicationUserId == userId &&
                        application.Status == ApplicationStatus.Approved));

            if (property == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            property.Title = model.Title.Trim();
            property.Description = model.Description.Trim();
            property.Address = model.Address.Trim();
            property.PricePerNight = model.PricePerNight!.Value;

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId,
                Action = $"Updated property {property.Id}",
                ActivityType = ActivityType.PropertyUpdated,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Your property was updated.";

            return RedirectToAction(nameof(Index));
        }
    }
}