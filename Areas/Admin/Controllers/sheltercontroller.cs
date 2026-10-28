using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;

using ShelterModel = PetWAMS.Web.Models.Shelter;

namespace PetWAMS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SheltersController : Controller
    {
        private readonly PetWamsDbContext _db;

        public SheltersController(PetWamsDbContext db)
        {
            _db = db;
        }

        // =========================
        // SHELTER LIST
        // =========================
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UserType") != "Admin")
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            var shelters = await _db.Shelters
                .OrderBy(s => s.ShelterID)
                .ToListAsync();

            return View(shelters);
        }

        // =========================
        // CREATE SHELTER
        // =========================
        [HttpGet]
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("UserType") != "Admin")
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ShelterModel model)
        {
            if (HttpContext.Session.GetString("UserType") != "Admin")
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            if (ModelState.IsValid)
            {
                _db.Shelters.Add(model);
                await _db.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(model);
        }

        // =========================
        // EDIT SHELTER
        // =========================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (HttpContext.Session.GetString("UserType") != "Admin")
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            var shelter = await _db.Shelters
                .FirstOrDefaultAsync(s => s.ShelterID == id);

            if (shelter == null)
            {
                return NotFound();
            }

            return View(shelter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ShelterModel model)
        {
            if (HttpContext.Session.GetString("UserType") != "Admin")
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            var shelter = await _db.Shelters
                .FirstOrDefaultAsync(s =>
                    s.ShelterID == model.ShelterID);

            if (shelter == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            shelter.ShelterName = model.ShelterName;
            shelter.Address = model.Address;
            shelter.ContactNumber = model.ContactNumber;
            shelter.Email = model.Email;
            shelter.OpenHours = model.OpenHours;

            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // =========================
        // DELETE SHELTER
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (HttpContext.Session.GetString("UserType") != "Admin")
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            var shelter = await _db.Shelters
                .FirstOrDefaultAsync(s => s.ShelterID == id);

            if (shelter == null)
            {
                return NotFound();
            }

            bool hasPets = await _db.Pets
                .AnyAsync(p => p.ShelterID == id);

            if (hasPets)
            {
                TempData["Error"] =
                    "This shelter cannot be deleted because pets are assigned to it.";

                return RedirectToAction("Index");
            }

            _db.Shelters.Remove(shelter);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}