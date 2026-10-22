using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class HostApplication
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string ApplicationUserId { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int PropertyId { get; set; }

        [EnumDataType(typeof(ApplicationStatus))]
        public ApplicationStatus Status { get; set; }

        public DateTime AppliedAt { get; set; } = DateTime.Now;
    }
}