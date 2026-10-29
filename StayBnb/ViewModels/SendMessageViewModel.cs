using System.ComponentModel.DataAnnotations;

namespace StayBnb.ViewModels
{
    public class SendMessageViewModel
    {
        [Required]
        [EmailAddress]
        [StringLength(256)]
        [Display(Name = "Recipient email")]
        public string RecipientEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(4000)]
        [Display(Name = "Message")]
        public string Content { get; set; } = string.Empty;
    }
}