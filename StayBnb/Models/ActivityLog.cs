using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class ActivityLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [EnumDataType(typeof(ActivityType))]
        public ActivityType ActivityType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}