using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class WishlistItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int PropertyId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
