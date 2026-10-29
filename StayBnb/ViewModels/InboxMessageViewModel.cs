namespace StayBnb.ViewModels
{
    public class InboxMessageViewModel
    {
        public string SenderEmail { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }
    }
}