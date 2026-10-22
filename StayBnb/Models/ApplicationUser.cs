using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class ApplicationUser
    {
        [Key]
        [Required]
        [StringLength(450)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ProfilePictureUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}