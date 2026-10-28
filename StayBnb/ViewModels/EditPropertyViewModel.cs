using System.ComponentModel.DataAnnotations;

namespace StayBnb.ViewModels
{
    public class EditPropertyViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [Range(typeof(decimal), "0.01", "999999.99", ParseLimitsInInvariantCulture = true, ErrorMessage = "Enter a price between 0.01 and 999999.99.")]
        [Display(Name = "Price per night")]
        public decimal? PricePerNight { get; set; }
    }
}