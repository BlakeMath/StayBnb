using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Range(1, int.MaxValue)]
        public int PropertyId { get; set; }

        [Required]
        [StringLength(450)]
        public string ReviewerId { get; set; } = string.Empty;

        [Range(1, 5, ErrorMessage = "The rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [Required]
        [StringLength(1000)]
        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}