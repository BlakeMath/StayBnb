using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StayBnb.Data;
using StayBnb.Models;
using StayBnb.ViewModels;
using System.Security.Claims;

namespace StayBnb.Controllers
{
    [Authorize(Roles = "Guest")]
    public class GuestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public GuestController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
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
