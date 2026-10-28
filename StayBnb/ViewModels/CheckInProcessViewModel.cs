using System.ComponentModel.DataAnnotations;

namespace StayBnb.ViewModels
{
    public class CheckInProcessViewModel
    {
        public int PropertyId { get; set; }

        public string PropertyTitle { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Check-in title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        [Display(Name = "Instructions")]
        public string Instructions { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(ID|Passport|Either)$",ErrorMessage = "Select a valid document requirement.")]
        [Display(Name = "Required document")]
        public string RequiredDocumentType { get; set; } = "Either";
    }
}