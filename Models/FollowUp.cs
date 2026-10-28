#nullable enable

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetWAMS.Web.Models
{
    [Table("FollowUps")]
    public class FollowUp
    {
        [Key]
        public int FollowUpID { get; set; }

        public int AdoptionID { get; set; }

        public DateTime? FollowUpDate { get; set; }

        public string? HealthCondition { get; set; }

        public string? Remarks { get; set; }

        public Adoption? Adoption { get; set; }
    }
}