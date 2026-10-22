using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class HostProperty
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Precision(18, 2)]
        [Range(typeof(decimal),"0.01", "999999.99", ErrorMessage = "The price per night must be greater than zero and less than 1 million.")]
        public decimal PricePerNight { get; set; }

        [Required]
        [StringLength(450)]
        public string HostId { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        public string Address { get; set; } = string.Empty;
    }
}