using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetWAMS.Web.Models
{
    [Table("AdopterVerifications")]
    public class AdopterVerification
    {
        [Key]
        public int VerificationID { get; set; }


        // =============================================
        // ADOPTER INFORMATION
        // =============================================

        [Required]
        public int UserID { get; set; }

        [Required]
        [StringLength(150)]
        public string FullNameOnNID { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }


        // =============================================
        // NID INFORMATION
        // =============================================

        /*
            Full NID number plain text হিসেবে রাখা হবে না।
            ASP.NET Data Protection দিয়ে encrypt করার পর
            এই column-এ রাখা হবে।
        */
        [Required]
        [StringLength(1000)]
        public string NIDNumberEncrypted { get; set; } = string.Empty;


        /*
            SHA-256 hash রাখা হবে।
            একই NID দিয়ে একাধিক account verification
            আটকানোর কাজে ব্যবহার হবে।
        */
        [Required]
        [StringLength(64)]
        public string NIDNumberHash { get; set; } = string.Empty;


        /*
            Admin list এবং adopter profile-এ masked NID
            দেখানোর জন্য শুধু শেষ ৪টি সংখ্যা রাখা হবে।
            Example: **********1234
        */
        [Required]
        [StringLength(4)]
        public string NIDLastFour { get; set; } = string.Empty;


        // NID images public wwwroot folder-এ রাখা হবে না।
        [Required]
        [StringLength(300)]
        public string NIDFrontImagePath { get; set; } = string.Empty;

        [StringLength(300)]
        public string? NIDBackImagePath { get; set; }


        // =============================================
        // VERIFICATION INFORMATION
        // =============================================

        /*
            Possible values:
            Pending
            Approved
            Rejected
        */
        [Required]
        [StringLength(30)]
        public string VerificationStatus { get; set; } = "Pending";

        [StringLength(500)]
        public string? AdminRemarks { get; set; }

        public int? VerifiedByAdminUserID { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        public DateTime? VerifiedAt { get; set; }


        // =============================================
        // ADOPTION POLICY ACCEPTANCE
        // =============================================

        public bool PolicyAccepted { get; set; } = false;

        [Required]
        [StringLength(20)]
        public string PolicyVersion { get; set; } = "1.0";

        public DateTime? PolicyAcceptedAt { get; set; }


        // =============================================
        // RELATIONSHIP
        // =============================================

        [ForeignKey(nameof(UserID))]
        public User? User { get; set; }


        // =============================================
        // DISPLAY-ONLY PROPERTY
        // Database column তৈরি হবে না।
        // =============================================

        [NotMapped]
        public string MaskedNID
        {
            get
            {
                if (string.IsNullOrWhiteSpace(NIDLastFour))
                {
                    return "Not Available";
                }

                return "**********" + NIDLastFour;
            }
        }
    }
}