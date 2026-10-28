using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PetWAMS.Web.ViewModels
{
    public class AdopterVerificationViewModel
    {
        // Stores the selected pet during the verification process.
        public int? PetID { get; set; }

        // Displays the selected pet name on the verification page.
        public string? SelectedPetName { get; set; }

        [Required(ErrorMessage = "Full name on NID is required.")]
        [StringLength(
            150,
            ErrorMessage = "Full name cannot exceed 150 characters.")]
        [Display(Name = "Full Name on NID")]
        public string FullNameOnNID { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of birth is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "NID number is required.")]
        [RegularExpression(
            @"^(?:\d{10}|\d{13}|\d{17})$",
            ErrorMessage =
                "NID number must contain 10, 13, or 17 digits.")]
        [Display(Name = "NID Number")]
        public string NIDNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "NID front image is required.")]
        [Display(Name = "NID Front Image")]
        public IFormFile? NIDFrontImage { get; set; }

        [Display(Name = "NID Back Image")]
        public IFormFile? NIDBackImage { get; set; }

        [Range(
            typeof(bool),
            "true",
            "true",
            ErrorMessage =
                "You must accept the adoption policy.")]
        [Display(Name = "Accept Adoption Policy")]
        public bool PolicyAccepted { get; set; }

        public bool HasExistingVerification { get; set; }

        public string? CurrentStatus { get; set; }

        public string? MaskedNID { get; set; }

        public string? AdminRemarks { get; set; }

        public DateTime? SubmittedAt { get; set; }
    }
}