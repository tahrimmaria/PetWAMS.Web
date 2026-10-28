using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetWAMS.Web.Models
{
    [Table("PetCategories")]
    public class PetCategory
    {
        [Key] public int CategoryID { get; set; }
        [Required] public string CategoryName { get; set; }   // Dog / Cat / Bird
    }
}
