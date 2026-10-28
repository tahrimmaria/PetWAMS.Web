using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetWAMS.Web.Models
{
    [Table("Shelters")]
    public class Shelter
    {
        [Key]
        public int ShelterID { get; set; }

        [Required]
        public string ShelterName { get; set; }

        public string? Address { get; set; }

        public string? ContactNumber { get; set; }

        public string? Email { get; set; }

        public string? OpenHours { get; set; }

        public string ApprovalStatus { get; set; } = "Approved";
    }
}