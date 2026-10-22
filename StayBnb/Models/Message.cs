using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string SenderId { get; set; } = string.Empty;

        [Required]
        [StringLength(450)]
        public string ReceiverId { get; set; } = string.Empty;

        [Required]
        [StringLength(4000)]
        public string Content { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}