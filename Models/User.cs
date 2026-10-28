using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetWAMS.Web.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        // Existing role names:
        // Admin   = Admin user
        // Adopter = Adopter user
        // Staff   = Shelter / Staff user
        public string UserType { get; set; } = string.Empty;

        // Existing shelter mapping used by Shelter controllers.
        // Staff/Shelter users may be connected with one Shelter.
        public int? ShelterID { get; set; }

        // Existing approval system used by Admin > Users page.
        // Example values: Pending, Approved, Rejected
        public string ApprovalStatus { get; set; } = "Pending";

        // New column for unified role-based login.
        // Password is stored as hash, not plain text.
        public string PasswordHash { get; set; } = string.Empty;

        // Navigation properties
        public Shelter? Shelter { get; set; }

        public ICollection<AdoptionRequest> AdoptionRequests { get; set; } =
            new List<AdoptionRequest>();

        public ICollection<Adoption> Adoptions { get; set; } =
            new List<Adoption>();
    }
}