using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetWAMS.Web.Models
{
    public class PaymentTransaction
    {
        [Key]
        public int PaymentTransactionID { get; set; }


        // Which user paid
        public int UserID { get; set; }


        // Related shop order
        public int OrderID { get; set; }


        // Amount paid
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }


        // SSLCommerz / Cash / Other
        public string? PaymentMethod { get; set; }


        // Paid / Failed / Pending
        public string? TransactionStatus { get; set; }


        // Gateway transaction id
        public string? GatewayTransactionID { get; set; }


        public DateTime TransactionDate { get; set; } = DateTime.Now;



        // Navigation

        public virtual User? User { get; set; }

        public virtual PetShopOrder? Order { get; set; }
    }
}
