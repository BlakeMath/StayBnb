using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class GuestDocument
    {
        [Key]
        public int Id { get; set; }

        [Range(1, int.MaxValue)]
        public int GuestCheckInId { get; set; }

        [Required]
        [StringLength(100)]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string FileName { get; set; } = string.Empty;

        [EnumDataType(typeof(DocumentStatus))]
        public DocumentStatus Status { get; set; }
    }
}