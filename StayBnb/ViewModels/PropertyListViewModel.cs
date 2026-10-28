namespace StayBnb.ViewModels
{
    public class PropertyListViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public decimal PricePerNight { get; set; }

        public string? ImageUrl { get; set; }
    }
}