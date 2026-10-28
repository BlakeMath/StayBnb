using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnb.Data;
using StayBnb.Models;

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
    }
}