using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetWAMS.Web.Models
{
    [Table("PetShopProductVariants")]
    public class PetShopProductVariant
    {
        [Key]
        public int ProductVariantID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [Required]
        [StringLength(100)]
        public string VariantName { get; set; } = string.Empty;

        [StringLength(30)]
        public string? ColorCode { get; set; }

        [StringLength(350)]
        public string? ImagePath { get; set; }

        [Required]
        public int StockQuantity { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ProductID")]
        public PetShopProduct? Product { get; set; }
    }
}