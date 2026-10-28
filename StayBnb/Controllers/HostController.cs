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
        public async Task<IActionResult> BookingRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var requests = await (
                from booking in _context.Bookings
                join property in _context.HostProperties
                    on booking.PropertyId equals property.Id
                join guest in _context.Users
                    on booking.GuestId equals guest.Id
                where property.HostId == userId &&
                      booking.Status == BookingStatus.Pending
                orderby booking.Id
                select new HostBookingListViewModel
                {
                    BookingId = booking.Id,
                    PropertyTitle = property.Title,
                    GuestEmail = guest.Email ?? string.Empty,
                    CheckInDate = booking.CheckInDate,
                    CheckOutDate = booking.CheckOutDate,
                    NumberOfTravelers = booking.NumberOfTravelers,
                    TotalPrice = booking.TotalPrice
                })
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewBooking(int id, string decision)
        {
            if (decision != "Approve" && decision != "Reject")
            {
                return BadRequest();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var booking = await _context.Bookings.FirstOrDefaultAsync(booking =>
                    booking.Id == id &&
                    _context.HostProperties.Any(property =>
                        property.Id == booking.PropertyId &&
                        property.HostId == userId));

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status != BookingStatus.Pending)
            {
                TempData["Error"] = "This booking has already been reviewed.";
                return RedirectToAction(nameof(BookingRequests));
            }

            if (decision == "Approve")
            {
                var propertyAvailable = await _context.HostProperties
                    .AnyAsync(property =>
                        property.Id == booking.PropertyId &&
                        property.HostId == userId &&
                        property.IsActive &&
                        _context.HostApplications.Any(application =>
                            application.PropertyId == property.Id &&
                            application.ApplicationUserId == userId &&
                            application.Status == ApplicationStatus.Approved));

                if (!propertyAvailable)
                {
                    TempData["Error"] = "The property must be approved and active before approving a booking.";

                    return RedirectToAction(nameof(BookingRequests));
                }

                if (booking.CheckInDate.Date < DateTime.Today ||
                    booking.CheckOutDate.Date <= booking.CheckInDate.Date)
                {
                    TempData["Error"] = "This request has invalid or past dates and cannot be approved.";

                    return RedirectToAction(nameof(BookingRequests));
                }

                var overlaps = await _context.Bookings.AnyAsync(other =>
                    other.Id != booking.Id &&
                    other.PropertyId == booking.PropertyId &&
                    (other.Status == BookingStatus.Approved ||
                     other.Status == BookingStatus.CheckedIn) &&
                    other.CheckInDate < booking.CheckOutDate &&
                    other.CheckOutDate > booking.CheckInDate);

                if (overlaps)
                {
                    TempData["Error"] = "Those dates overlap an existing confirmed booking. You can reject this request.";

                    return RedirectToAction(nameof(BookingRequests));
                }

                booking.Status = BookingStatus.Approved;
            }
            else
            {
                booking.Status = BookingStatus.Rejected;
            }

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId,
                Action = $"Booking {booking.Id}: {booking.Status}",
                ActivityType = ActivityType.BookingStatusChanged,
                CreatedAt = DateTime.Now
            });

            _context.Notifications.Add(new Notification
            {
                UserId = booking.GuestId,
                Title = $"Booking {booking.Id}: {booking.Status}",
                Type = NotificationType.BookingUpdate,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = $"Booking {booking.Id} was {booking.Status.ToString().ToLowerInvariant()}.";

            return RedirectToAction(nameof(BookingRequests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetActive(int id, bool isActive)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var property = await _context.HostProperties
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

            if (property.IsActive != isActive)
            {
                property.IsActive = isActive;

                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userId,
                    Action = $"Property {property.Id} " + (isActive ? "activated" : "deactivated"),
                    ActivityType = ActivityType.PropertyUpdated,
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
            }

            TempData["Success"] = isActive ? "Your property is now active." : "Your property is now inactive.";

            return RedirectToAction(nameof(Index));
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