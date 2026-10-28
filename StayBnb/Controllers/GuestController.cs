using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StayBnb.Data;
using StayBnb.Models;
using StayBnb.ViewModels;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace StayBnb.Controllers
{
    [Authorize(Roles = "Guest")]
    public class GuestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public GuestController(ApplicationDbContext context, IWebHostEnvironment environment, IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> MyBookings()
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
                where booking.GuestId == userId
                orderby booking.Id descending
                select new GuestBookingListViewModel
                {
                    BookingId = booking.Id,
                    PropertyTitle = property.Title,
                    CheckInDate = booking.CheckInDate,
                    CheckOutDate = booking.CheckOutDate,
                    NumberOfTravelers = booking.NumberOfTravelers,
                    TotalPrice = booking.TotalPrice,
                    Status = booking.Status
                })
                .ToListAsync();

            return View(bookings);
        }

        [HttpGet]
        public async Task<IActionResult> Properties(string? search)
        {
            var query = _context.HostProperties
                .AsNoTracking()
                .Where(property =>
                    property.IsActive &&
                    _context.HostApplications.Any(application =>
                        application.PropertyId == property.Id &&
                        application.ApplicationUserId == property.HostId &&
                        application.Status == ApplicationStatus.Approved));

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(property => property.Address.Contains(search));
            }

            var properties = await query
                .OrderBy(property => property.Title)
                .ThenBy(property => property.Id)
                .Select(property => new PropertyListViewModel
                {
                    Id = property.Id,
                    Title = property.Title,
                    Address = property.Address,
                    PricePerNight = property.PricePerNight,
                    ImageUrl = _context.PropertyImages
                        .Where(image => image.PropertyId == property.Id)
                        .OrderBy(image => image.Id)
                        .Select(image => image.ImageUrl)
                        .FirstOrDefault()
                })
                .ToListAsync();

            ViewData["Search"] = search;

            return View(properties);
        }

        [HttpGet]
        public async Task<IActionResult> PropertyDetails(int id)
        {
            var property = await _context.HostProperties
                .AsNoTracking()
                .Where(property =>
                    property.Id == id &&
                    property.IsActive &&
                    _context.HostApplications.Any(application =>
                        application.PropertyId == property.Id &&
                        application.ApplicationUserId == property.HostId &&
                        application.Status == ApplicationStatus.Approved))
                .Select(property => new PropertyDetailsViewModel
                {
                    Id = property.Id,
                    Title = property.Title,
                    Description = property.Description,
                    Address = property.Address,
                    PricePerNight = property.PricePerNight,
                    ImageUrls = _context.PropertyImages
                        .Where(image => image.PropertyId == property.Id)
                        .OrderBy(image => image.Id)
                        .Select(image => image.ImageUrl)
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (property == null)
            {
                return NotFound();
            }

            return View(property);
        }

        [HttpGet]
        public async Task<IActionResult> Book(int id)
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
                    property.IsActive &&
                    property.HostId != userId &&
                    _context.HostApplications.Any(application =>
                        application.PropertyId == property.Id &&
                        application.ApplicationUserId == property.HostId &&
                        application.Status == ApplicationStatus.Approved));

            if (property == null)
            {
                return NotFound();
            }

            var cleaningFee = _configuration.GetValue<decimal?>("BookingFees:CleaningFee");

            var serviceFee =
                _configuration.GetValue<decimal?>("BookingFees:ServiceFee");

            if (!cleaningFee.HasValue || !serviceFee.HasValue || cleaningFee.Value < 0 || serviceFee.Value < 0)
            {
                throw new InvalidOperationException("Configure valid, non-negative booking fees in appsettings.json.");
            }

            var model = new CreateBookingViewModel
            {
                PropertyId = property.Id,
                PropertyTitle = property.Title,
                PricePerNight = property.PricePerNight,
                CleaningFee = cleaningFee.Value,
                ServiceFee = serviceFee.Value,
                CheckInDate = DateTime.Today.AddDays(1),
                CheckOutDate = DateTime.Today.AddDays(2),
                NumberOfTravelers = 1
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitBooking(
        [Bind("PropertyId,CheckInDate,CheckOutDate,NumberOfTravelers")]
        CreateBookingViewModel model, string? quotedTotal)
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
                    property.IsActive &&
                    property.HostId != userId &&
                    _context.HostApplications.Any(application =>
                        application.PropertyId == property.Id &&
                        application.ApplicationUserId == property.HostId &&
                        application.Status == ApplicationStatus.Approved));

            if (property == null)
            {
                return NotFound();
            }

            var cleaningFee = _configuration.GetValue<decimal?>("BookingFees:CleaningFee");

            var serviceFee = _configuration.GetValue<decimal?>("BookingFees:ServiceFee");

            if (!cleaningFee.HasValue || !serviceFee.HasValue || cleaningFee.Value < 0 || serviceFee.Value < 0)
            {
                throw new InvalidOperationException("Configure valid, non-negative booking fees in appsettings.json.");
            }

            // Recalculate using current server-side prices.
            model.PropertyTitle = property.Title;
            model.PricePerNight = property.PricePerNight;
            model.CleaningFee = cleaningFee.Value;
            model.ServiceFee = serviceFee.Value;

            if (!ModelState.IsValid)
            {
                return View("Book", model);
            }

            if (model.TotalPrice > 999999999.99m)
            {
                ModelState.AddModelError( string.Empty, "The total exceeds the supported booking amount.");

                return View("Book", model);
            }

            // Ask the Guest to review again if the price changed.
            if (!decimal.TryParse( quotedTotal, NumberStyles.Number, CultureInfo.InvariantCulture, out var previousTotal) || previousTotal != model.TotalPrice)
            {
                ModelState.AddModelError(string.Empty, "The price has changed or the quote is missing. Review the total and submit again.");

                return View("ReviewBooking", model);
            }

            var checkIn = model.CheckInDate!.Value.Date;
            var checkOut = model.CheckOutDate!.Value.Date;

            var unavailable = await _context.Bookings.AnyAsync(booking =>
                booking.PropertyId == property.Id &&
                (booking.Status == BookingStatus.Approved ||
                 booking.Status == BookingStatus.CheckedIn) &&
                booking.CheckInDate < checkOut &&
                booking.CheckOutDate > checkIn);

            if (unavailable)
            {
                ModelState.AddModelError(string.Empty, "This property is unavailable for those dates. Choose another stay.");

                return View("Book", model);
            }

            var duplicate = await _context.Bookings.AnyAsync(booking =>
                booking.PropertyId == property.Id &&
                booking.GuestId == userId &&
                booking.Status == BookingStatus.Pending &&
                booking.CheckInDate == checkIn &&
                booking.CheckOutDate == checkOut);

            if (duplicate)
            {
                ModelState.AddModelError(string.Empty, "You already have a pending request for this property and these dates.");

                return View("Book", model);
            }

            var booking = new Booking
            {
                PropertyId = property.Id,
                GuestId = userId,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                NumberOfTravelers = model.NumberOfTravelers!.Value,
                CleaningFee = model.CleaningFee,
                ServiceFee = model.ServiceFee,
                TotalPrice = model.TotalPrice,
                Status = BookingStatus.Pending
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId,
                Action = $"Submitted booking request {booking.Id}",
                ActivityType = ActivityType.BookingCreated,
                CreatedAt = DateTime.Now
            });

            _context.Notifications.Add(new Notification
            {
                UserId = property.HostId,
                Title = $"New booking request {booking.Id} for property {property.Id}",
                Type = NotificationType.BookingUpdate,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = $"Booking request {booking.Id} was submitted. Awaiting Host approval.";

            return RedirectToAction(nameof(MyBookings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book([Bind("PropertyId,CheckInDate,CheckOutDate,NumberOfTravelers")]CreateBookingViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var property = await _context.HostProperties
                .AsNoTracking()
                .FirstOrDefaultAsync(property =>
                    property.Id == model.PropertyId &&
                    property.IsActive &&
                    property.HostId != userId &&
                    _context.HostApplications.Any(application =>
                        application.PropertyId == property.Id &&
                        application.ApplicationUserId == property.HostId &&
                        application.Status == ApplicationStatus.Approved));

            if (property == null)
            {
                return NotFound();
            }

            var cleaningFee = _configuration.GetValue<decimal?>("BookingFees:CleaningFee");

            var serviceFee = _configuration.GetValue<decimal?>("BookingFees:ServiceFee");

            if (!cleaningFee.HasValue || !serviceFee.HasValue || cleaningFee.Value < 0 || serviceFee.Value < 0)
            {
                throw new InvalidOperationException("Configure valid, non-negative booking fees in appsettings.json.");
            }

            // Prices from server-side configuration to prevent tampering.
            model.PropertyTitle = property.Title;
            model.PricePerNight = property.PricePerNight;
            model.CleaningFee = cleaningFee.Value;
            model.ServiceFee = serviceFee.Value;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.TotalPrice > 999999999.99m)
            {
                ModelState.AddModelError(string.Empty, "The total exceeds the supported booking amount. Select a shorter stay.");

                return View(model);
            }

            return View("ReviewBooking", model);
        }

        [HttpGet]
        public IActionResult ApplyToHost()
        {
            return View(new HostApplicationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyToHost(HostApplicationViewModel model)
        {
            const long maxImageSize = 5 * 1024 * 1024;

            if (model.Image != null)
            {
                if (model.Image.Length == 0)
                {
                    ModelState.AddModelError(nameof(model.Image),"The selected image is empty.");
                }
                else if (model.Image.Length > maxImageSize)
                {
                    ModelState.AddModelError(nameof(model.Image), "The image must be 5 MB or smaller.");
                }
                else if (!await IsSupportedImageAsync(model.Image))
                {
                    ModelState.AddModelError(nameof(model.Image), "Select a valid JPG or PNG image.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var image = model.Image!;
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";

            var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "properties");

            Directory.CreateDirectory(uploadFolder);

            var filePath = Path.Combine(uploadFolder, fileName);

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Save the uploaded image.
                await using (var stream = new FileStream(filePath, FileMode.CreateNew))
                {
                    await image.CopyToAsync(stream);
                }

                // Save the property first so SQL Server generates its ID.
                var property = new HostProperty
                {
                    Title = model.Title.Trim(),
                    Description = model.Description.Trim(),
                    PricePerNight = model.PricePerNight!.Value,
                    Address = model.Address.Trim(),
                    HostId = userId
                };

                _context.HostProperties.Add(property);
                await _context.SaveChangesAsync();

                // Link the saved image to the property.
                _context.PropertyImages.Add(new PropertyImage
                {
                    PropertyId = property.Id,
                    ImageUrl = $"/uploads/properties/{fileName}"
                });

                // Create the pending application.
                _context.HostApplications.Add(new HostApplication
                {
                    ApplicationUserId = userId,
                    PropertyId = property.Id,
                    Status = ApplicationStatus.Pending,
                    AppliedAt = DateTime.Now
                });

                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userId,
                    Action = $"Submitted Host application for property {property.Id}",
                    ActivityType = ActivityType.HostApplicationSubmitted,
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                // Database rollback does not remove files from disk.
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                throw;
            }

            TempData["Success"] =
                "Your Host application has been submitted for review.";

            return RedirectToAction(nameof(Index));
        }

        private static async Task<bool> IsSupportedImageAsync(IFormFile image)
        {
            if (image.Length < 8)
            {
                return false;
            }

            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

            byte[] header = new byte[8];

            using var stream = image.OpenReadStream();
            await stream.ReadExactlyAsync(header);

            if (extension == ".jpg" || extension == ".jpeg")
            {
                return header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
            }

            if (extension == ".png")
            {
                byte[] pngHeader = {0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A};

                return header.SequenceEqual(pngHeader);
            }

            return false;
        }
    }
}
