using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class PropertyImage
    {
        [Key]
        public int Id { get; set; }

        [Range(1, int.MaxValue)]
        public int PropertyId { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;
    }
}