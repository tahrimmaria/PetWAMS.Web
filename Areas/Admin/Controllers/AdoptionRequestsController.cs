using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Models;

namespace PetWAMS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdoptionRequestsController : Controller
    {
        private readonly PetWamsDbContext _db;

        public AdoptionRequestsController(PetWamsDbContext db)
        {
            _db = db;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("UserType") == "Admin";
        }

        // =========================
        // REQUEST LIST
        // =========================
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            var requests = await _db.AdoptionRequests
                .AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.Pet)
                .OrderByDescending(r => r.RequestID)
                .ToListAsync();

            // Get the adopter IDs connected with the displayed requests.
            var adopterUserIds = requests
                .Select(r => r.UserID)
                .Distinct()
                .ToList();

            // Load verification information for the connected adopters.
            var adopterVerifications = await _db.AdopterVerifications
                .AsNoTracking()
                .Where(v => adopterUserIds.Contains(v.UserID))
                .ToDictionaryAsync(v => v.UserID);

            // The existing AdoptionRequest model remains unchanged.
            // The view will use this dictionary to show NID verification status.
            ViewBag.AdopterVerifications = adopterVerifications;

            return View(requests);
        }

        // =========================
        // APPROVE REQUEST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            var request = await _db.AdoptionRequests
                .Include(r => r.Pet)
                .FirstOrDefaultAsync(r => r.RequestID == id);

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

            // Check the adopter verification before approval.
            var adopterVerification = await _db.AdopterVerifications
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.UserID == request.UserID);

            if (adopterVerification == null)
            {
                TempData["Error"] =
                    "This adopter has not submitted identity verification.";

                return RedirectToAction("Index");
            }

            if (!string.Equals(
                    adopterVerification.VerificationStatus,
                    "Verified",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "The adopter identity must be verified before approving this request.";

                return RedirectToAction("Index");
            }

            if (!adopterVerification.PolicyAccepted)
            {
                TempData["Error"] =
                    "The adopter has not accepted the adoption safety policy.";

                return RedirectToAction("Index");
            }

            if (request.Pet == null)
            {
                TempData["Error"] =
                    "The pet linked to this request was not found.";

                return RedirectToAction("Index");
            }

            bool alreadyAdopted = await _db.Adoptions
                .AnyAsync(a => a.PetID == request.PetID);

            if (alreadyAdopted ||
                string.Equals(
                    request.Pet.Status,
                    "Adopted",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "This pet has already been adopted.";

                return RedirectToAction("Index");
            }

            // Approve the selected request.
            request.Status = "Approved";

            // Change the pet status.
            request.Pet.Status = "Adopted";

            // Create the final adoption record.
            var adoption = new Adoption
            {
                PetID = request.PetID,
                UserID = request.UserID,
                AdoptionDate = DateTime.Now
            };

            _db.Adoptions.Add(adoption);

            // Reject other pending requests for the same pet.
            var otherRequests = await _db.AdoptionRequests
                .Where(r =>
                    r.PetID == request.PetID &&
                    r.RequestID != request.RequestID &&
                    r.Status == "Pending")
                .ToListAsync();

            foreach (var otherRequest in otherRequests)
            {
                otherRequest.Status = "Rejected";
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
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            var request = await _db.AdoptionRequests
                .FirstOrDefaultAsync(r => r.RequestID == id);

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