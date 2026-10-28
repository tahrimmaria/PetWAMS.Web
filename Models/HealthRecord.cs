using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetWAMS.Web.Models
{
    [Table("HealthRecords")]
    public class HealthRecord
    {
        [Key] public int HealthID { get; set; }
        public int PetID { get; set; }
        public string HealthStatus { get; set; }   // "Healthy" / "Not Healthy"
        public string MedicalNotes { get; set; }
        public DateTime? CheckupDate { get; set; }

        // Added via additional_schema.sql
        public bool Vaccinated { get; set; }

        [ForeignKey("PetID")]
        public Pet Pet { get; set; }
    }
}
