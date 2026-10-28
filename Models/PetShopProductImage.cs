using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetWAMS.Web.Models
{
    [Table("PetShopProductImages")]
    public class PetShopProductImage
    {
        [Key]
        public int ProductImageID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [Required]
        [StringLength(350)]
        public string ImagePath { get; set; } = string.Empty;

        public bool IsPrimary { get; set; } = false;

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        [ForeignKey("ProductID")]
        public PetShopProduct? Product { get; set; }
    }
}