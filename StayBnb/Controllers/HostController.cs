using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnb.Data;
using StayBnb.Models;
using StayBnb.ViewModels;
using System.Security.Claims;
using System.Text.Json;

namespace StayBnb.Controllers
{
    [Authorize(Roles = "Host")]
    public class HostController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public HostController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var properties = await _context.HostProperties.AsNoTracking().Where(property =>
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
        public async Task<IActionResult> CheckInSubmissions()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var documents = await (
                from document in _context.GuestDocuments
                join checkIn in _context.GuestCheckIns
                    on document.GuestCheckInId equals checkIn.Id
                join booking in _context.Bookings
                    on checkIn.BookingId equals booking.Id
                join property in _context.HostProperties
                    on booking.PropertyId equals property.Id
                join guest in _context.Users
                    on booking.GuestId equals guest.Id
                where property.HostId == userId &&
                      booking.Status == BookingStatus.Approved &&
                      checkIn.Status == CheckInStatus.Submitted &&
                      document.Status == DocumentStatus.Pending
                orderby booking.Id, document.Id
                select new HostDocumentReviewViewModel
                {
                    DocumentId = document.Id,
                    BookingId = booking.Id,
                    PropertyTitle = property.Title,
                    GuestEmail = guest.Email ?? string.Empty,
                    DocumentType = document.DocumentType
                })
                .ToListAsync();

            return View(documents);
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> DownloadGuestDocument(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var document = await (
                from item in _context.GuestDocuments
                join checkIn in _context.GuestCheckIns
                    on item.GuestCheckInId equals checkIn.Id
                join booking in _context.Bookings
                    on checkIn.BookingId equals booking.Id
                join property in _context.HostProperties
                    on booking.PropertyId equals property.Id
                where item.Id == id &&
                      property.HostId == userId
                select item)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (document == null)
            {
                return NotFound();
            }

            // Only allow a filename, never a folder path.
            if (string.IsNullOrWhiteSpace(document.FileName) || Path.GetFileName(document.FileName) != document.FileName)
            {
                return NotFound();
            }

            var extension = Path.GetExtension(document.FileName).ToLowerInvariant();

            string contentType;

            if (extension == ".jpg" || extension == ".jpeg")
            {
                contentType = "image/jpeg";
            }
            else if (extension == ".png")
            {
                contentType = "image/png";
            }
            else
            {
                return NotFound();
            }

            var filePath = Path.Combine(_environment.ContentRootPath, "App_Data", "GuestDocuments", document.FileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            Response.Headers["X-Content-Type-Options"] = "nosniff";

            return PhysicalFile(filePath, contentType, $"Document-{document.Id}{extension}");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewGuestDocument(int documentId, bool approve)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var submission = await (
                from document in _context.GuestDocuments
                join checkIn in _context.GuestCheckIns
                    on document.GuestCheckInId equals checkIn.Id
                join booking in _context.Bookings
                    on checkIn.BookingId equals booking.Id
                join property in _context.HostProperties
                    on booking.PropertyId equals property.Id
                where document.Id == documentId &&
                      property.HostId == userId &&
                      booking.Status == BookingStatus.Approved &&
                      checkIn.Status == CheckInStatus.Submitted &&
                      document.Status == DocumentStatus.Pending
                select new
                {
                    Document = document,
                    CheckIn = checkIn,
                    Booking = booking
                })
                .FirstOrDefaultAsync();

            if (submission == null)
            {
                return NotFound();
            }

            submission.Document.Status = approve ? DocumentStatus.Verified : DocumentStatus.Rejected;

            submission.CheckIn.Status = approve ? CheckInStatus.Verified : CheckInStatus.Rejected;

            var decision = approve ? "Verified" : "Rejected";

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId,
                Action = $"{decision} document for booking {submission.Booking.Id}.",
                ActivityType = ActivityType.DocumentReviewed,
                CreatedAt = DateTime.Now
            });

            _context.Notifications.Add(new Notification
            {
                UserId = submission.Booking.GuestId,
                Title = $"Check-in {decision.ToLowerInvariant()} for booking {submission.Booking.Id}.",
                Type = NotificationType.CheckInUpdate,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = approve ? "The Guest's document has been verified." : "The document was rejected. The Guest can upload a replacement.";

            return RedirectToAction(nameof(CheckInSubmissions));
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
                    TotalPrice = booking.TotalPrice,
                    Status = booking.Status
                })
                .ToListAsync();

            return View(requests);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmedBookings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var bookings = await (
                from booking in _context.Bookings
                join property in _context.HostProperties
                    on booking.PropertyId equals property.Id
                join guest in _context.Users
                    on booking.GuestId equals guest.Id
                where property.HostId == userId &&
                      (booking.Status == BookingStatus.Approved ||
                       booking.Status == BookingStatus.CheckedIn ||
                       booking.Status == BookingStatus.Completed)
                orderby booking.CheckInDate, booking.Id
                select new HostBookingListViewModel
                {
                    BookingId = booking.Id,
                    PropertyTitle = property.Title,
                    GuestEmail = guest.Email ?? string.Empty,
                    CheckInDate = booking.CheckInDate,
                    CheckOutDate = booking.CheckOutDate,
                    NumberOfTravelers = booking.NumberOfTravelers,
                    TotalPrice = booking.TotalPrice,
                    Status = booking.Status
                })
                .ToListAsync();

            return View(bookings);
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

                if (booking.CheckInDate.Date < DateTime.Today || booking.CheckOutDate.Date <= booking.CheckInDate.Date)
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

            var property = await _context.HostProperties.AsNoTracking().FirstOrDefaultAsync(property =>
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

            var property = await _context.HostProperties.FirstOrDefaultAsync(property =>
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

        [HttpGet]
        public async Task<IActionResult> ConfigureCheckIn(int id)
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

            var process = await _context.CheckInProcesses
                .AsNoTracking()
                .FirstOrDefaultAsync(process => process.PropertyId == id);

            var model = new CheckInProcessViewModel
            {
                PropertyId = property.Id,
                PropertyTitle = property.Title
            };

            if (process != null)
            {
                var instructions = JsonSerializer.Deserialize<CheckInInstructions>(process.StepsJson)
                    ?? throw new InvalidOperationException("The saved check-in instructions are invalid.");

                model.Title = process.Title;
                model.Instructions = string.Join(Environment.NewLine, instructions.Steps);
                model.RequiredDocumentType = instructions.RequiredDocumentType;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfigureCheckIn([Bind("PropertyId,Title,Instructions,RequiredDocumentType")] CheckInProcessViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var property = await _context.HostProperties
                .AsNoTracking()
                .FirstOrDefaultAsync(property =>
                    property.Id == model.PropertyId &&
                    property.HostId == userId &&
                    _context.HostApplications.Any(application =>
                        application.PropertyId == property.Id &&
                        application.ApplicationUserId == userId &&
                        application.Status == ApplicationStatus.Approved));

            if (property == null)
            {
                return NotFound();
            }

            model.PropertyTitle = property.Title;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var instructions = new CheckInInstructions
            {
                Steps = model.Instructions.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),

                RequiredDocumentType = model.RequiredDocumentType
            };

            if (instructions.Steps.Count == 0)
            {
                ModelState.AddModelError(nameof(model.Instructions), "Enter at least one instruction.");

                return View(model);
            }

            var stepsJson = JsonSerializer.Serialize(instructions);

            if (stepsJson.Length > 10000)
            {
                ModelState.AddModelError(nameof(model.Instructions), "The saved instructions are too long. Shorten them.");

                return View(model);
            }

            var process = await _context.CheckInProcesses.FirstOrDefaultAsync(process => process.PropertyId == property.Id);

            if (process != null &&
                await _context.GuestCheckIns.AnyAsync(checkIn => checkIn.CheckInProcessId == process.Id))
            {
                ModelState.AddModelError(string.Empty, "These instructions are already being used by a Guest check-in and cannot be changed.");

                return View(model);
            }

            if (process == null)
            {
                process = new CheckInProcess
                {
                    PropertyId = property.Id,
                    CreatedAt = DateTime.Now
                };

                _context.CheckInProcesses.Add(process);
            }

            process.Title = model.Title.Trim();
            process.StepsJson = stepsJson;

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId,
                Action = $"Configured check-in for property {property.Id}",
                ActivityType = ActivityType.CheckInUpdated,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = "Check-in instructions saved.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkArrived(int bookingId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var booking = await _context.Bookings.FirstOrDefaultAsync(booking =>
                    booking.Id == bookingId &&
                    booking.Status == BookingStatus.Approved &&
                    _context.HostProperties.Any(property =>
                        property.Id == booking.PropertyId &&
                        property.HostId == userId));

            if (booking == null)
            {
                return NotFound();
            }

            var verified = await _context.GuestCheckIns.AnyAsync(checkIn => checkIn.BookingId == booking.Id && checkIn.Status == CheckInStatus.Verified);

            if (!verified)
            {
                TempData["Error"] = "Verify the Guest's document before marking arrival.";

                return RedirectToAction(nameof(ConfirmedBookings));
            }

            if (DateTime.Today < booking.CheckInDate.Date || DateTime.Today >= booking.CheckOutDate.Date)
            {
                TempData["Error"] = "Arrival can only be recorded during the booked stay.";

                return RedirectToAction(nameof(ConfirmedBookings));
            }

            booking.Status = BookingStatus.CheckedIn;

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId,
                Action = $"Marked booking {booking.Id} as CheckedIn.",
                ActivityType = ActivityType.BookingStatusChanged,
                CreatedAt = DateTime.Now
            });

            _context.Notifications.Add(new Notification
            {
                UserId = booking.GuestId,
                Title = $"Arrival confirmed for booking {booking.Id}.",
                Type = NotificationType.BookingUpdate,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = "The Guest has been marked as arrived.";

            return RedirectToAction(nameof(ConfirmedBookings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkDeparted(int bookingId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var booking = await _context.Bookings.FirstOrDefaultAsync(booking =>
                    booking.Id == bookingId &&
                    booking.Status == BookingStatus.CheckedIn &&
                    _context.HostProperties.Any(property =>
                        property.Id == booking.PropertyId &&
                        property.HostId == userId));

            if (booking == null)
            {
                return NotFound();
            }

            booking.Status = BookingStatus.Completed;

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId,
                Action = $"Marked booking {booking.Id} as Completed.",
                ActivityType = ActivityType.BookingStatusChanged,
                CreatedAt = DateTime.Now
            });

            _context.Notifications.Add(new Notification
            {
                UserId = booking.GuestId,
                Title = $"Stay completed for booking {booking.Id}.",
                Type = NotificationType.BookingUpdate,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = "The Guest's stay has been completed.";

            return RedirectToAction(nameof(ConfirmedBookings));
        }
    }
}