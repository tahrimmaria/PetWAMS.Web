using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetWAMS.Web.Models
{
    [Table("Adoptions")]
    public class Adoption
    {
        [Key] public int AdoptionID { get; set; }
        public int? PetID { get; set; }
        public int? UserID { get; set; }
        public DateTime? AdoptionDate { get; set; }

        [ForeignKey("PetID")] public Pet Pet { get; set; }
        [ForeignKey("UserID")] public User User { get; set; }
    }
}
