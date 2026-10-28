using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetWAMS.Web.Models
{
    [Table("AdopterPreferences")]
    public class AdopterPreference
    {
        [Key]
        public int PreferenceID { get; set; }

        [Required]
        public int UserID { get; set; }

        // Dog / Cat / Other / Any
        public int? PreferredCategoryID { get; set; }

        // Male / Female / Any
        public string? PreferredGender { get; set; }

        // Preferred age range
        public int? MinimumAge { get; set; }

        public int? MaximumAge { get; set; }

        // Optional breed preference
        public string? PreferredBreed { get; set; }

        // Apartment / House
        public string? LivingType { get; set; }

        // Low / Medium / High
        public string? ActivityLevel { get; set; }

        // None / Beginner / Experienced
        public string? PetExperience { get; set; }

        public bool? HasChildren { get; set; }

        // Extra information for AI analysis
        public string? AdditionalNotes { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;


        // =========================
        // RELATIONSHIPS
        // =========================

        [ForeignKey("UserID")]
        public User? User { get; set; }

        [ForeignKey("PreferredCategoryID")]
        public PetCategory? PreferredCategory { get; set; }
    }
}