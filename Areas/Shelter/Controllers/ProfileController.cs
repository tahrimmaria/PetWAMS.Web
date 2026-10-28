using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;

namespace PetWAMS.Web.Areas.Shelter.Controllers
{
    [Area("Shelter")]
    public class ProfileController : Controller
    {
        private readonly PetWamsDbContext _db;

        public ProfileController(PetWamsDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var userType = HttpContext.Session.GetString("UserType");

            if (userId == null || userType != "Staff")
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new { area = "" }
                );
            }

            var staff = await _db.Users
                .Include(u => u.Shelter)
                .FirstOrDefaultAsync(u => u.UserID == userId.Value);

            if (staff == null)
            {
                return NotFound();
            }

            if (staff.ShelterID == null || staff.Shelter == null)
            {
                ViewBag.Error =
                    "No shelter has been assigned to this staff account.";

                return View(null);
            }

            int shelterId = staff.ShelterID.Value;

            ViewBag.TotalPets = await _db.Pets
                .CountAsync(p => p.ShelterID == shelterId);

            ViewBag.TotalAdoptions = await _db.Adoptions
                .Include(a => a.Pet)
                .CountAsync(a =>
                    a.Pet != null &&
                    a.Pet.ShelterID == shelterId);

            return View(staff.Shelter);
        }
    }
}