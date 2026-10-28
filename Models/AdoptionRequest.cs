using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetWAMS.Web.Models
{
    [Table("AdoptionRequests")]
    public class AdoptionRequest
    {
        [Key] public int RequestID { get; set; }
        public int? UserID { get; set; }
        public int? PetID { get; set; }
        public DateTime? RequestDate { get; set; }
        public string Status { get; set; }   // Pending / Approved / Rejected

        [ForeignKey("UserID")] public User User { get; set; }
        [ForeignKey("PetID")] public Pet Pet { get; set; }
    }
}
