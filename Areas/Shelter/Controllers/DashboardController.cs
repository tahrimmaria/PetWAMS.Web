using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;

namespace PetWAMS.Web.Areas.Shelter.Controllers
{
    [Area("Shelter")]
    public class DashboardController : Controller
    {
        private readonly PetWamsDbContext _db;

        public DashboardController(PetWamsDbContext db)
        {
            _db = db;
        }

        private bool IsShelterLoggedIn()
        {
            string? userType = HttpContext.Session.GetString("UserType");

            return userType != null &&
                   (
                       userType.Equals("Staff", StringComparison.OrdinalIgnoreCase) ||
                       userType.Equals("Shelter", StringComparison.OrdinalIgnoreCase)
                   );
        }

        private async Task<int?> GetCurrentShelterIdAsync()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return null;
            }

            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserID == userId.Value);

            if (user == null)
            {
                return null;
            }

            if (user.ShelterID == null || user.ShelterID <= 0)
            {
                return null;
            }

            return user.ShelterID;
        }

        public async Task<IActionResult> Index()
        {
            if (!IsShelterLoggedIn())
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }

            string fullName =
                HttpContext.Session.GetString("FullName")
                ?? "Shelter User";

            ViewBag.FullName = fullName;

            int? shelterId = await GetCurrentShelterIdAsync();

            if (shelterId == null)
            {
                ViewBag.TotalPets = 0;
                ViewBag.PendingRequests = 0;
                ViewBag.TotalAdoptions = 0;
                ViewBag.NeedingCheckup = 0;
                ViewBag.RecentRequests = new List<object>();
                ViewBag.RecentPets = new List<object>();
                ViewBag.Error = "This staff account is not linked with any shelter.";

                return View();
            }

            var petsQuery = _db.Pets
                .Include(p => p.Category)
                .Where(p => p.ShelterID == shelterId.Value);

            var requestsQuery = _db.AdoptionRequests
                .Include(r => r.User)
                .Include(r => r.Pet)
                    .ThenInclude(p => p.Category)
                .Where(r =>
                    r.Pet != null &&
                    r.Pet.ShelterID == shelterId.Value);

            var adoptionsQuery = _db.Adoptions
                .Include(a => a.Pet)
                .Where(a =>
                    a.Pet != null &&
                    a.Pet.ShelterID == shelterId.Value);

            var healthQuery = _db.HealthRecords
                .Include(h => h.Pet)
                .Where(h =>
                    h.Pet != null &&
                    h.Pet.ShelterID == shelterId.Value);

            ViewBag.TotalPets = await petsQuery.CountAsync();

            ViewBag.PendingRequests = await requestsQuery
                .CountAsync(r =>
                    r.Status != null &&
                    r.Status.ToLower() == "pending");

            ViewBag.TotalAdoptions = await adoptionsQuery.CountAsync();

            DateTime oldCheckupDate = DateTime.Today.AddMonths(-6);

            ViewBag.NeedingCheckup = await healthQuery
                .CountAsync(h =>
                    h.Vaccinated == false ||
                    h.CheckupDate == null ||
                    h.CheckupDate < oldCheckupDate);

            ViewBag.RecentRequests = await requestsQuery
                .OrderByDescending(r => r.RequestDate)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentPets = await petsQuery
                .OrderByDescending(p => p.PetID)
                .Take(5)
                .ToListAsync();

            ViewBag.CurrentShelterID = shelterId.Value;

            return View();
        }
    }
}