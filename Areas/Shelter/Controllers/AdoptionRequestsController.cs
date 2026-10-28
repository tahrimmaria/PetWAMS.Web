using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Models;

namespace PetWAMS.Web.Areas.Shelter.Controllers
{
    [Area("Shelter")]
    public class AdoptionRequestsController : Controller
    {
        private readonly PetWamsDbContext _db;

        public AdoptionRequestsController(PetWamsDbContext db)
        {
            _db = db;
        }

        private bool IsStaff()
        {
            return HttpContext.Session.GetInt32("UserID") != null &&
                   HttpContext.Session.GetString("UserType") == "Staff";
        }

        private async Task<int?> GetStaffShelterId()
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
                return null;

            return await _db.Users
                .Where(u => u.UserID == userId.Value)
                .Select(u => u.ShelterID)
                .FirstOrDefaultAsync();
        }


        // =========================
        // ADOPTION REQUEST LIST
        // =========================
        public async Task<IActionResult> Index()
        {
            if (!IsStaff())
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new { area = "" }
                );
            }

            var shelterId = await GetStaffShelterId();

            if (shelterId == null)
            {
                return Content(
                    "No shelter has been assigned to this staff account."
                );
            }

            var requests = await _db.AdoptionRequests
                .Include(r => r.User)
                .Include(r => r.Pet)
                .Where(r =>
                    r.Pet != null &&
                    r.Pet.ShelterID == shelterId.Value)
                .OrderByDescending(r => r.RequestID)
                .ToListAsync();

            return View(requests);
        }


        // =========================
        // APPROVE REQUEST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            if (!IsStaff())
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new { area = "" }
                );
            }

            var shelterId = await GetStaffShelterId();

            if (shelterId == null)
            {
                return Unauthorized();
            }

            var request = await _db.AdoptionRequests
                .Include(r => r.Pet)
                .FirstOrDefaultAsync(r =>
                    r.RequestID == id &&
                    r.Pet != null &&
                    r.Pet.ShelterID == shelterId.Value);

            if (request == null)
            {
                return NotFound();
            }

            if (!string.Equals(
                    request.Status,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Only pending requests can be approved.";

                return RedirectToAction("Index");
            }

            bool alreadyAdopted = await _db.Adoptions
                .AnyAsync(a => a.PetID == request.PetID);

            if (alreadyAdopted ||
                string.Equals(
                    request.Pet?.Status,
                    "Adopted",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "This pet has already been adopted.";

                return RedirectToAction("Index");
            }

            request.Status = "Approved";

            if (request.Pet != null)
            {
                request.Pet.Status = "Adopted";
            }

            var adoption = new Adoption
            {
                PetID = request.PetID,
                UserID = request.UserID,
                AdoptionDate = DateTime.Now
            };

            _db.Adoptions.Add(adoption);

            // Same pet-এর অন্য pending requests reject
            var otherRequests = await _db.AdoptionRequests
                .Where(r =>
                    r.PetID == request.PetID &&
                    r.RequestID != request.RequestID &&
                    r.Status == "Pending")
                .ToListAsync();

            foreach (var other in otherRequests)
            {
                other.Status = "Rejected";
            }

            await _db.SaveChangesAsync();

            TempData["Success"] =
                "Adoption request approved successfully.";

            return RedirectToAction("Index");
        }


        // =========================
        // REJECT REQUEST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            if (!IsStaff())
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new { area = "" }
                );
            }

            var shelterId = await GetStaffShelterId();

            if (shelterId == null)
            {
                return Unauthorized();
            }

            var request = await _db.AdoptionRequests
                .Include(r => r.Pet)
                .FirstOrDefaultAsync(r =>
                    r.RequestID == id &&
                    r.Pet != null &&
                    r.Pet.ShelterID == shelterId.Value);

            if (request == null)
            {
                return NotFound();
            }

            if (!string.Equals(
                    request.Status,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Only pending requests can be rejected.";

                return RedirectToAction("Index");
            }

            request.Status = "Rejected";

            await _db.SaveChangesAsync();

            TempData["Success"] =
                "Adoption request rejected successfully.";

            return RedirectToAction("Index");
        }
    }
}