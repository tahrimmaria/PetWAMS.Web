using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetWAMS.Web.Models
{
    [Table("PetRecommendations")]
    public class PetRecommendation
    {
        [Key]
        public int RecommendationID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public int PetID { get; set; }

        [Required]
        public int MatchScore { get; set; }

        public string? RecommendationReason { get; set; }

        public bool IsViewed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;


        // =========================
        // RELATIONSHIPS
        // =========================

        [ForeignKey("UserID")]
        public User? User { get; set; }

        [ForeignKey("PetID")]
        public Pet? Pet { get; set; }
    }
}