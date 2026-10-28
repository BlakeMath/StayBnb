using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace StayBnb.ViewModels
{
    public class HostApplicationViewModel
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Property title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(typeof(decimal), "0.01", "999999.99", ParseLimitsInInvariantCulture = true, ErrorMessage = "Enter a price between 0.01 and 999999.99.")]
        [Display(Name = "Price per night")]
        public decimal? PricePerNight { get; set; }

        [Required]
        [StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a property image.")]
        [Display(Name = "Property image")]
        public IFormFile? Image { get; set; }
    }
}