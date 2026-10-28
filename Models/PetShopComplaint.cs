using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetWAMS.Web.Models
{
    [Table("PetShopComplaints")]
    public class PetShopComplaint
    {
        [Key]
        public int ComplaintID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public int OrderID { get; set; }

        [Required]
        [StringLength(100)]
        public string ComplaintType { get; set; } = string.Empty;

        [Required]
        [StringLength(700)]
        public string ComplaintMessage { get; set; } = string.Empty;

        [StringLength(30)]
        public string ComplaintStatus { get; set; } = "Pending";

        [StringLength(700)]
        public string? AdminReply { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ResolvedAt { get; set; }

        [ForeignKey("UserID")]
        public User? User { get; set; }

        [ForeignKey("OrderID")]
        public PetShopOrder? Order { get; set; }
    }
}