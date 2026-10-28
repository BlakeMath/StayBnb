namespace StayBnb.ViewModels
{
    public class HostDocumentReviewViewModel
    {
        public int DocumentId { get; set; }

        public int BookingId { get; set; }

        public string PropertyTitle { get; set; } = string.Empty;

        public string GuestEmail { get; set; } = string.Empty;

        public string DocumentType { get; set; } = string.Empty;
    }
}