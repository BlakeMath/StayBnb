using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace StayBnb.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Range(1, int.MaxValue)]
        public int BookingId { get; set; }

        [Precision(18, 2)]
        [Range(typeof(decimal), "0.01", "9999999999999999.99")]
        public decimal Amount { get; set; }

        [EnumDataType(typeof(PaymentStatus))]
        public PaymentStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}