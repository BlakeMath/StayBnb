using StayBnb.Models;

namespace StayBnb.ViewModels
{
    public class HostBookingListViewModel
    {
        public int BookingId { get; set; }

        public string PropertyTitle { get; set; } = string.Empty;

        public string GuestEmail { get; set; } = string.Empty;

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public int NumberOfTravelers { get; set; }

        public decimal TotalPrice { get; set; }

        public BookingStatus Status { get; set; }
    }
}