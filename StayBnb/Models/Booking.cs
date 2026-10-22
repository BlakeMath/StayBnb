using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class Booking : IValidatableObject
    {
        [Key]
        public int Id { get; set; }

        [Range(1, int.MaxValue)]
        public int PropertyId { get; set; }

        [Required]
        [StringLength(450)]
        public string GuestId { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime CheckInDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime CheckOutDate { get; set; }

        [Precision(18, 2)]
        [Range(typeof(decimal), "0.01", "999999999.99", ErrorMessage = "The total price must be greater than zero.")]
        public decimal TotalPrice { get; set; }

        [EnumDataType(typeof(BookingStatus))]
        public BookingStatus Status { get; set; }

        
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Check if CheckInDate is not set (default value)
            if (CheckInDate == default) 
            {
                yield return new ValidationResult( "A check-in date is required.", new[] { nameof(CheckInDate) });
            }

            // Check if CheckOutDate is not set (default value)
            if (CheckOutDate == default) 
            {
                yield return new ValidationResult("A check-out date is required.", new[] { nameof(CheckOutDate) });
            }

            // Check if CheckOutDate is not after CheckInDate
            if (CheckInDate != default && CheckOutDate != default && CheckOutDate.Date <= CheckInDate.Date) 
            {
                yield return new ValidationResult("The check-out date must be after the check-in date.", new[] { nameof(CheckOutDate) });
            }
        }
    }
}