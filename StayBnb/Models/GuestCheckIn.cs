using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class GuestCheckIn
    {
        [Key]
        public int Id { get; set; }

        [Range(1, int.MaxValue)]
        public int BookingId { get; set; }

        [Range(1, int.MaxValue)]
        public int CheckInProcessId { get; set; }

        [EnumDataType(typeof(CheckInStatus))]
        public CheckInStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}