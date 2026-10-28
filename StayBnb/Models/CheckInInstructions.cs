namespace StayBnb.Models
{
    public class CheckInInstructions
    {
        public List<string> Steps { get; set; } = new();

        public string RequiredDocumentType { get; set; } = "Either";
    }
}