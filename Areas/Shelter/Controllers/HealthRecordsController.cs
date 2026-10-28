using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Models;

namespace PetWAMS.Web.Areas.Shelter.Controllers
{
    [Area("Shelter")]
    public class HealthRecordsController : Controller
    {
        private readonly PetWamsDbContext _db;

        public HealthRecordsController(PetWamsDbContext db)
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
        // HEALTH RECORD LIST
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

            var records = await _db.HealthRecords
                .Include(h => h.Pet)
                .Where(h =>
                    h.Pet != null &&
                    h.Pet.ShelterID == shelterId.Value)
                .OrderByDescending(h => h.CheckupDate)
                .ToListAsync();

            return View(records);
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

            // Only this shelter's pets
            ViewBag.Pets = await _db.Pets
                .Where(p => p.ShelterID == shelterId.Value)
                .OrderBy(p => p.PetName)
                .ToListAsync();

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HealthRecord record)
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

            // Make sure selected pet belongs to staff's shelter
            bool ownsPet = await _db.Pets.AnyAsync(p =>
                p.PetID == record.PetID &&
                p.ShelterID == shelterId.Value);

            if (!ownsPet)
            {
                return BadRequest(
                    "You can only add health records for pets in your shelter."
                );
            }

            if (ModelState.IsValid)
            {
                _db.HealthRecords.Add(record);
                await _db.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            ViewBag.Pets = await _db.Pets
                .Where(p => p.ShelterID == shelterId.Value)
                .OrderBy(p => p.PetName)
                .ToListAsync();

            return View(record);
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

            var record = await _db.HealthRecords
                .Include(h => h.Pet)
                .FirstOrDefaultAsync(h =>
                    h.HealthID == id &&
                    h.Pet != null &&
                    h.Pet.ShelterID == shelterId.Value);

            if (record == null)
            {
                return NotFound();
            }

            ViewBag.Pets = await _db.Pets
                .Where(p => p.ShelterID == shelterId.Value)
                .OrderBy(p => p.PetName)
                .ToListAsync();

            return View(record);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(HealthRecord model)
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

            // Existing record must belong to own shelter
            var record = await _db.HealthRecords
                .Include(h => h.Pet)
                .FirstOrDefaultAsync(h =>
                    h.HealthID == model.HealthID &&
                    h.Pet != null &&
                    h.Pet.ShelterID == shelterId.Value);

            if (record == null)
            {
                return NotFound();
            }

            // New selected pet must also belong to own shelter
            bool ownsPet = await _db.Pets.AnyAsync(p =>
                p.PetID == model.PetID &&
                p.ShelterID == shelterId.Value);

            if (!ownsPet)
            {
                return BadRequest(
                    "You can only use pets from your own shelter."
                );
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Pets = await _db.Pets
                    .Where(p => p.ShelterID == shelterId.Value)
                    .OrderBy(p => p.PetName)
                    .ToListAsync();

                return View(model);
            }

            record.PetID = model.PetID;
            record.HealthStatus = model.HealthStatus;
            record.MedicalNotes = model.MedicalNotes;
            record.CheckupDate = model.CheckupDate;

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

            var record = await _db.HealthRecords
                .Include(h => h.Pet)
                .FirstOrDefaultAsync(h =>
                    h.HealthID == id &&
                    h.Pet != null &&
                    h.Pet.ShelterID == shelterId.Value);

            if (record == null)
            {
                return NotFound();
            }

            _db.HealthRecords.Remove(record);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}