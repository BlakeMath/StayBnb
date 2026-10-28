using StayBnb.Models;

namespace StayBnb.ViewModels
{
    public class GuestCheckInViewModel
    {
        public int BookingId { get; set; }

        public string PropertyTitle { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public List<string> Steps { get; set; } = new();

        public string RequiredDocumentType { get; set; } = string.Empty;

        public bool InstructionsAvailable { get; set; }

        public CheckInStatus? Status { get; set; }
    }
}