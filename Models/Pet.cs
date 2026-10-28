using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetWAMS.Web.Models
{
    [Table("Pets")]
    public class Pet
    {
        [Key] public int PetID { get; set; }
        [Required] public string PetName { get; set; }
        public string Breed { get; set; }
        public int? Age { get; set; }
        public string Gender { get; set; }
        public string Color { get; set; }
        public string Status { get; set; }   // Available / Pending / Adopted

        public int? ShelterID { get; set; }
        public int? CategoryID { get; set; }

        public string MicrochipNumber { get; set; }
        public DateTime? ArrivalDate { get; set; }
        public string AIRecommendation { get; set; }

        // Added via additional_schema.sql — the "About [Pet]" paragraph
        public string Description { get; set; }

        [ForeignKey("ShelterID")]
        public Shelter Shelter { get; set; }

        [ForeignKey("CategoryID")]
        public PetCategory Category { get; set; }

        public ICollection<HealthRecord> HealthRecords { get; set; }
    }
}
