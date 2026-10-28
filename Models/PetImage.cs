using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetWAMS.Web.Models
{
    [Table("PetImages")]
    public class PetImage
    {
        [Key]
        public int PetImageID { get; set; }

        [Required]
        public int PetID { get; set; }

        [Required]
        [StringLength(350)]
        public string ImagePath { get; set; } = string.Empty;

        public bool IsPrimary { get; set; } = false;

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        [ForeignKey("PetID")]
        public Pet? Pet { get; set; }
    }
}