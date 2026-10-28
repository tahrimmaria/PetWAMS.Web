using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetWAMS.Web.Models
{
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }

        [Required]
        public string Title { get; set; } = "";

        [Required]
        public string Message { get; set; } = "";

        [Required]
        public string Type { get; set; } = "";

        public int? RelatedID { get; set; }

        public string? Link { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}