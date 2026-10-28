using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetWAMS.Web.Models
{
    [Table("PetShopOrderItems")]
    public class PetShopOrderItem
    {
        [Key]
        public int OrderItemID { get; set; }

        [Required]
        public int OrderID { get; set; }

        [Required]
        public int ProductID { get; set; }

        public int? ProductVariantID { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? VariantName { get; set; }

        [StringLength(350)]
        public string? VariantImagePath { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }

        [ForeignKey("OrderID")]
        public PetShopOrder? Order { get; set; }

        [ForeignKey("ProductID")]
        public PetShopProduct? Product { get; set; }

        [ForeignKey("ProductVariantID")]
        public PetShopProductVariant? ProductVariant { get; set; }
    }
}