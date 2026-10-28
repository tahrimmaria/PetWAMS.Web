using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Models;

namespace PetWAMS.Web.Controllers
{
    public class AdoptionController : Controller
    {
        private readonly PetWamsDbContext _db;

        public AdoptionController(
            PetWamsDbContext db)
        {
            _db = db;
        }

        // Ensures that the adopter has a verified identity.
        private async Task<IActionResult?>
            RequireVerifiedIdentityAsync(
                int userId,
                int petId)
        {
            AdopterVerification? verification =
                await _db.AdopterVerifications
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item => item.UserID == userId);

            if (verification == null)
            {
                TempData["SuccessMessage"] =
                    "Submit your identity verification to continue. Your adoption request will be created automatically after submission.";

                return RedirectToAction(
                    "Index",
                    "Verification",
                    new
                    {
                        area = "Adopter",
                        petId
                    });
            }

            string status =
                verification.VerificationStatus
                ?? string.Empty;

            if (status.Equals(
                "Pending",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["SuccessMessage"] =
                    "Your identity verification and adoption request are waiting for administrator review.";

                return RedirectToAction(
                    "Index",
                    "Verification",
                    new
                    {
                        area = "Adopter",
                        petId
                    });
            }

            if (status.Equals(
                "Rejected",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["SuccessMessage"] =
                    "Your identity verification was rejected. Review the administrator remarks and submit the corrected information.";

                return RedirectToAction(
                    "Index",
                    "Verification",
                    new
                    {
                        area = "Adopter",
                        petId
                    });
            }

            if (!status.Equals(
                    "Verified",
                    StringComparison.OrdinalIgnoreCase)
                || !verification.PolicyAccepted)
            {
                TempData["SuccessMessage"] =
                    "Verified identity and an accepted adoption policy are required before applying.";

                return RedirectToAction(
                    "Index",
                    "Verification",
                    new
                    {
                        area = "Adopter",
                        petId
                    });
            }

            return null;
        }

        // Displays the adoption application form.
        [HttpGet]
        public async Task<IActionResult> Apply(
            int? petId,
            int? id)
        {
            int finalPetId =
                petId ?? id ?? 0;

            int? userId =
                HttpContext.Session.GetInt32("UserID");

            string? userType =
                HttpContext.Session.GetString("UserType");

            if (userId == null
                || string.IsNullOrWhiteSpace(userType)
                || !userType.Equals(
                    "Adopter",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "SignIn",
                    "Account");
            }

            if (finalPetId <= 0)
            {
                TempData["Error"] =
                    "Pet not found. Please select a pet again.";

                return RedirectToAction(
                    "Index",
                    "Pets");
            }

            Pet? pet =
                await _db.Pets
                    .Include(item => item.Category)
                    .FirstOrDefaultAsync(
                        item => item.PetID == finalPetId);

            if (pet == null)
            {
                TempData["Error"] =
                    "Pet not found. Please select a pet again.";

                return RedirectToAction(
                    "Index",
                    "Pets");
            }

            bool alreadyAdopted =
                await _db.Adoptions
                    .AnyAsync(
                        item =>
                            item.PetID == finalPetId);

            bool alreadyApproved =
                await _db.AdoptionRequests
                    .AnyAsync(item =>
                        item.PetID == finalPetId
                        && item.Status != null
                        && item.Status.ToLower()
                            == "approved");

            bool petStatusAdopted =
                !string.IsNullOrWhiteSpace(pet.Status)
                && pet.Status.Equals(
                    "Adopted",
                    StringComparison.OrdinalIgnoreCase);

            if (alreadyAdopted
                || alreadyApproved
                || petStatusAdopted)
            {
                TempData["Error"] =
                    "This pet has already been adopted or approved.";

                return RedirectToAction(
                    "Index",
                    "Pets");
            }

            IActionResult? verificationRedirect =
                await RequireVerifiedIdentityAsync(
                    userId.Value,
                    finalPetId);

            if (verificationRedirect != null)
            {
                return verificationRedirect;
            }

            bool alreadyRequestedByThisUser =
                await _db.AdoptionRequests
                    .AnyAsync(item =>
                        item.UserID == userId.Value
                        && item.PetID == finalPetId
                        && item.Status != null
                        &&
                        (
                            item.Status.ToLower()
                                == "pending"
                            ||
                            item.Status.ToLower()
                                == "approved"
                        ));

            if (alreadyRequestedByThisUser)
            {
                ViewBag.InfoMessage =
                    "You already submitted an adoption request for this pet. Please wait for administrator review.";
            }

            return View(pet);
        }

        // Creates a new request for an already verified adopter.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(
            int? petId,
            int? id,
            string? reason)
        {
            int finalPetId =
                petId ?? id ?? 0;

            int? userId =
                HttpContext.Session.GetInt32("UserID");

            string? userType =
                HttpContext.Session.GetString("UserType");

            if (userId == null
                || string.IsNullOrWhiteSpace(userType)
                || !userType.Equals(
                    "Adopter",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "SignIn",
                    "Account");
            }

            if (finalPetId <= 0)
            {
                TempData["Error"] =
                    "Pet not found. Please select a pet again.";

                return RedirectToAction(
                    "Index",
                    "Pets");
            }

            Pet? pet =
                await _db.Pets
                    .Include(item => item.Category)
                    .FirstOrDefaultAsync(
                        item => item.PetID == finalPetId);

            if (pet == null)
            {
                TempData["Error"] =
                    "Pet not found. Please select a pet again.";

                return RedirectToAction(
                    "Index",
                    "Pets");
            }

            bool alreadyAdopted =
                await _db.Adoptions
                    .AnyAsync(
                        item =>
                            item.PetID == finalPetId);

            bool alreadyApproved =
                await _db.AdoptionRequests
                    .AnyAsync(item =>
                        item.PetID == finalPetId
                        && item.Status != null
                        && item.Status.ToLower()
                            == "approved");

            bool petStatusAdopted =
                !string.IsNullOrWhiteSpace(pet.Status)
                && pet.Status.Equals(
                    "Adopted",
                    StringComparison.OrdinalIgnoreCase);

            if (alreadyAdopted
                || alreadyApproved
                || petStatusAdopted)
            {
                TempData["Error"] =
                    "This pet has already been adopted or approved.";

                return RedirectToAction(
                    "Index",
                    "Pets");
            }

            IActionResult? verificationRedirect =
                await RequireVerifiedIdentityAsync(
                    userId.Value,
                    finalPetId);

            if (verificationRedirect != null)
            {
                return verificationRedirect;
            }

            bool alreadyRequestedByThisUser =
                await _db.AdoptionRequests
                    .AnyAsync(item =>
                        item.UserID == userId.Value
                        && item.PetID == finalPetId
                        && item.Status != null
                        &&
                        (
                            item.Status.ToLower()
                                == "pending"
                            ||
                            item.Status.ToLower()
                                == "approved"
                        ));

            if (alreadyRequestedByThisUser)
            {
                ViewBag.InfoMessage =
                    "You already submitted an adoption request for this pet. Please wait for administrator review.";

                return View(pet);
            }

            var adoptionRequest =
                new AdoptionRequest
                {
                    UserID = userId.Value,
                    PetID = finalPetId,
                    RequestDate = DateTime.Now,
                    Status = "Pending"
                };

            _db.AdoptionRequests.Add(
                adoptionRequest);

            await _db.SaveChangesAsync();

            string adopterName =
                HttpContext.Session.GetString("FullName")
                ?? "An adopter";

            var notification =
                new Notification
                {
                    Title =
                        "New Adoption Request",

                    Message =
                        adopterName
                        + " requested to adopt "
                        + pet.PetName
                        + ".",

                    Link =
                        "/Admin/AdoptionRequests",

                    IsRead =
                        false,

                    CreatedAt =
                        DateTime.Now,

                    Type =
                        "AdoptionRequest",

                    RelatedID =
                        adoptionRequest.RequestID
                };

            _db.Notifications.Add(
                notification);

            await _db.SaveChangesAsync();

            ViewBag.SuccessMessage =
                "Your adoption request was submitted successfully.";

            ViewBag.RequestSubmitted =
                true;

            return View(pet);
        }
    }
}