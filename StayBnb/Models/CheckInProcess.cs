using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StayBnb.Models
{
    public class CheckInProcess
    {
        [Key]
        public int Id { get; set; }

        [Range(1, int.MaxValue)]
        public int PropertyId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(10000)]
        public string StepsJson { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}