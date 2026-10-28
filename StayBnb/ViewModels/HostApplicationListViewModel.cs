namespace StayBnb.ViewModels
{
    public class HostApplicationListViewModel
    {
        public int ApplicationId { get; set; }

        public string PropertyTitle { get; set; } = string.Empty;

        public string ApplicantEmail { get; set; } = string.Empty;

        public DateTime AppliedAt { get; set; }

        public string Description { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public decimal PricePerNight { get; set; }

        public List<string> ImageUrls { get; set; } = new();
    }
}