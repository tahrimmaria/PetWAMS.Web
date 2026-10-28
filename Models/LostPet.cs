using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetWAMS.Web.Models
{
    [Table("LostPets")]
    public class LostPet
    {
        [Key]
        public int LostID { get; set; }

        public int PetID { get; set; }

        public string? LostLocation { get; set; }

        public DateTime? LostDate { get; set; }

        public string? ContactNumber { get; set; }

        public int? UserID { get; set; }

        public Pet? Pet { get; set; }

        public User? User { get; set; }
    }
}