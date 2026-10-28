using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Models;

namespace PetWAMS.Web.Areas.Shelter.Controllers
{
    [Area("Shelter")]
    public class FollowUpsController : Controller
    {
        private readonly PetWamsDbContext _db;

        public FollowUpsController(PetWamsDbContext db)
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
        // FOLLOW-UP LIST
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

            var followUps = await _db.FollowUps
                .Include(f => f.Adoption)
                    .ThenInclude(a => a.Pet)
                .Include(f => f.Adoption)
                    .ThenInclude(a => a.User)
                .Where(f =>
                    f.Adoption != null &&
                    f.Adoption.Pet != null &&
                    f.Adoption.Pet.ShelterID == shelterId.Value)
                .OrderByDescending(f => f.FollowUpID)
                .ToListAsync();

            return View(followUps);
        }


        // =========================
        // CREATE
        // =========================
        [HttpGet]
        public async Task<IActionResult> Create()
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

            // Only adoptions from this shelter
            ViewBag.Adoptions = await _db.Adoptions
                .Include(a => a.Pet)
                .Include(a => a.User)
                .Where(a =>
                    a.Pet != null &&
                    a.Pet.ShelterID == shelterId.Value)
                .OrderByDescending(a => a.AdoptionID)
                .ToListAsync();

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FollowUp model)
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

            // Selected adoption must belong to own shelter
            bool ownsAdoption = await _db.Adoptions
                .AnyAsync(a =>
                    a.AdoptionID == model.AdoptionID &&
                    a.Pet != null &&
                    a.Pet.ShelterID == shelterId.Value);

            if (!ownsAdoption)
            {
                return BadRequest(
                    "You can only create follow-ups for adoptions from your shelter."
                );
            }

            if (ModelState.IsValid)
            {
                _db.FollowUps.Add(model);
                await _db.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            ViewBag.Adoptions = await _db.Adoptions
                .Include(a => a.Pet)
                .Include(a => a.User)
                .Where(a =>
                    a.Pet != null &&
                    a.Pet.ShelterID == shelterId.Value)
                .OrderByDescending(a => a.AdoptionID)
                .ToListAsync();

            return View(model);
        }


        // =========================
        // EDIT
        // =========================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
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

            var followUp = await _db.FollowUps
                .Include(f => f.Adoption)
                    .ThenInclude(a => a.Pet)
                .FirstOrDefaultAsync(f =>
                    f.FollowUpID == id &&
                    f.Adoption != null &&
                    f.Adoption.Pet != null &&
                    f.Adoption.Pet.ShelterID == shelterId.Value);

            if (followUp == null)
            {
                return NotFound();
            }

            ViewBag.Adoptions = await _db.Adoptions
                .Include(a => a.Pet)
                .Include(a => a.User)
                .Where(a =>
                    a.Pet != null &&
                    a.Pet.ShelterID == shelterId.Value)
                .OrderByDescending(a => a.AdoptionID)
                .ToListAsync();

            return View(followUp);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FollowUp model)
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

            // Existing follow-up must belong to own shelter
            var followUp = await _db.FollowUps
                .Include(f => f.Adoption)
                    .ThenInclude(a => a.Pet)
                .FirstOrDefaultAsync(f =>
                    f.FollowUpID == model.FollowUpID &&
                    f.Adoption != null &&
                    f.Adoption.Pet != null &&
                    f.Adoption.Pet.ShelterID == shelterId.Value);

            if (followUp == null)
            {
                return NotFound();
            }

            // Selected adoption must also belong to own shelter
            bool ownsAdoption = await _db.Adoptions
                .AnyAsync(a =>
                    a.AdoptionID == model.AdoptionID &&
                    a.Pet != null &&
                    a.Pet.ShelterID == shelterId.Value);

            if (!ownsAdoption)
            {
                return BadRequest(
                    "You can only use adoptions from your own shelter."
                );
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Adoptions = await _db.Adoptions
                    .Include(a => a.Pet)
                    .Include(a => a.User)
                    .Where(a =>
                        a.Pet != null &&
                        a.Pet.ShelterID == shelterId.Value)
                    .OrderByDescending(a => a.AdoptionID)
                    .ToListAsync();

                return View(model);
            }

            followUp.AdoptionID = model.AdoptionID;
            followUp.FollowUpDate = model.FollowUpDate;
            followUp.HealthCondition = model.HealthCondition;
            followUp.Remarks = model.Remarks;

            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }


        // =========================
        // DELETE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
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

            var followUp = await _db.FollowUps
                .Include(f => f.Adoption)
                    .ThenInclude(a => a.Pet)
                .FirstOrDefaultAsync(f =>
                    f.FollowUpID == id &&
                    f.Adoption != null &&
                    f.Adoption.Pet != null &&
                    f.Adoption.Pet.ShelterID == shelterId.Value);

            if (followUp == null)
            {
                return NotFound();
            }

            _db.FollowUps.Remove(followUp);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}