using System.ComponentModel.DataAnnotations;

namespace PetWAMS.Web.ViewModels
{
    public class AdminAdopterVerificationViewModel
    {
        public int VerificationID { get; set; }

        public int UserID { get; set; }

        public string AdopterName { get; set; } =
            string.Empty;

        public string AdopterEmail { get; set; } =
            string.Empty;

        public string AdopterPhone { get; set; } =
            string.Empty;

        public string FullNameOnNID { get; set; } =
            string.Empty;

        public DateTime? DateOfBirth { get; set; }

        public string MaskedNID { get; set; } =
            string.Empty;

        public string FullNID { get; set; } =
            string.Empty;

        public string VerificationStatus { get; set; } =
            string.Empty;

        public string AdminRemarks { get; set; } =
            string.Empty;

        public DateTime SubmittedAt { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public bool PolicyAccepted { get; set; }

        public string PolicyVersion { get; set; } =
            string.Empty;

        public bool HasFrontImage { get; set; }

        public bool HasBackImage { get; set; }
    }

    public class AdminVerificationDecisionViewModel
    {
        [Required]
        public int VerificationID { get; set; }

        [StringLength(
            500,
            ErrorMessage =
                "Admin remarks cannot exceed 500 characters.")]
        public string? AdminRemarks { get; set; }
    }
}