using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetWAMS.Web.Models
{
    public class Invoice
    {
        [Key]
        public int InvoiceID { get; set; }


        public int OrderID { get; set; }


        public int UserID { get; set; }


        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;


        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }


        public string PaymentMethod { get; set; } = string.Empty;


        public string PaymentStatus { get; set; } = "Paid";


        public DateTime InvoiceDate { get; set; } = DateTime.Now;



        // Navigation

        public virtual PetShopOrder? Order { get; set; }

        public virtual User? User { get; set; }
    }
}