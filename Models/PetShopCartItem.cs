using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetWAMS.Web.Models
{
    [Table("PetShopCartItems")]
    public class PetShopCartItem
    {
        [Key]
        public int CartItemID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public int ProductID { get; set; }

        public int? ProductVariantID { get; set; }

        [Required]
        public int Quantity { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserID")]
        public User? User { get; set; }

        [ForeignKey("ProductID")]
        public PetShopProduct? Product { get; set; }

        [ForeignKey("ProductVariantID")]
        public PetShopProductVariant? ProductVariant { get; set; }
    }
}