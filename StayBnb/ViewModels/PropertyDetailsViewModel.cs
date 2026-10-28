namespace StayBnb.ViewModels
{
    public class PropertyDetailsViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public decimal PricePerNight { get; set; }

        public List<string> ImageUrls { get; set; } = new();
    }
}