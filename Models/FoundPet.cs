using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetWAMS.Web.Models
{
    [Table("FoundPets")]
    public class FoundPet
    {
        [Key]
        public int FoundID { get; set; }

        public int PetID { get; set; }

        public string? FoundLocation { get; set; }

        public DateTime? FoundDate { get; set; }

        public string? ReporterName { get; set; }

        public string? ContactNumber { get; set; }

        public int? UserID { get; set; }

        public Pet? Pet { get; set; }

        public User? User { get; set; }
    }
}