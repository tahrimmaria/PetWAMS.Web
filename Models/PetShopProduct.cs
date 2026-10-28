using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetWAMS.Web.Models
{
    [Table("PetShopProducts")]
    public class PetShopProduct
    {
        [Key]
        public int ProductID { get; set; }

        [Required]
        [StringLength(150)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        public string ProductCategory { get; set; } = string.Empty;

        [StringLength(50)]
        public string? PetType { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OriginalPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountPrice { get; set; }

        public int StockQuantity { get; set; } = 0;

        public int LowStockLimit { get; set; } = 5;

        [StringLength(30)]
        public string ApprovalStatus { get; set; } = "Pending";

        public bool IsActive { get; set; } = true;

        [StringLength(300)]
        public string? ImagePath { get; set; }

        [StringLength(700)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<PetShopCartItem> CartItems { get; set; } = new List<PetShopCartItem>();

        public ICollection<PetShopOrderItem> OrderItems { get; set; } = new List<PetShopOrderItem>();
    }
}