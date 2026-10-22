using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class Amenity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? IconClass { get; set; }
    }
}