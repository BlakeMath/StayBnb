using System.ComponentModel.DataAnnotations;

namespace StayBnb.ViewModels
{
    public class CreateBookingViewModel : IValidatableObject
    {
        public int PropertyId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Check-in date")]
        public DateTime? CheckInDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Check-out date")]
        public DateTime? CheckOutDate { get; set; }

        [Required]
        [Range(1,int.MaxValue, ErrorMessage = "At least one traveler is required.")]
        [Display(Name = "Number of travelers")]
        public int? NumberOfTravelers { get; set; } = 1;

        // Display values supplied by the server.
        public string PropertyTitle { get; set; } = string.Empty;

        public decimal PricePerNight { get; set; }

        public decimal CleaningFee { get; set; }

        public decimal ServiceFee { get; set; }

        public int Nights => CheckInDate.HasValue && CheckOutDate.HasValue? (CheckOutDate.Value.Date - CheckInDate.Value.Date).Days : 0;

        public decimal AccommodationTotal => PricePerNight * Nights;

        public decimal TotalPrice => AccommodationTotal + CleaningFee + ServiceFee;

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (CheckInDate.HasValue && CheckInDate.Value.Date < DateTime.Today)
            {
                yield return new ValidationResult("The check-in date cannot be in the past.", new[] { nameof(CheckInDate) });
            }

            if (CheckInDate.HasValue && CheckOutDate.HasValue && CheckOutDate.Value.Date <= CheckInDate.Value.Date)
            {
                yield return new ValidationResult("The check-out date must be after the check-in date.", new[] { nameof(CheckOutDate) });
            }
        }
    }
}