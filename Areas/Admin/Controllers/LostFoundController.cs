using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Models;

namespace PetWAMS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class LostFoundController : Controller
    {
        private readonly PetWamsDbContext _db;

        public LostFoundController(PetWamsDbContext db)
        {
            _db = db;
        }

        // =========================
        // LOST + FOUND LIST
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

            ViewBag.LostPets = await _db.LostPets
                .Include(x => x.Pet)
                .OrderByDescending(x => x.LostID)
                .ToListAsync();

            ViewBag.FoundPets = await _db.FoundPets
                .Include(x => x.Pet)
                .OrderByDescending(x => x.FoundID)
                .ToListAsync();

            return View();
        }

        // =========================
        // CREATE LOST
        // =========================
        [HttpGet]
        public async Task<IActionResult> CreateLost()
        {
            if (HttpContext.Session.GetString("UserType") != "Admin")
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            ViewBag.Pets = await _db.Pets
                .OrderBy(p => p.PetName)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLost(LostPet model)
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
                model.UserID = null;

                _db.LostPets.Add(model);
                await _db.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            ViewBag.Pets = await _db.Pets
                .OrderBy(p => p.PetName)
                .ToListAsync();

            return View(model);
        }

        // =========================
        // EDIT LOST
        // =========================
        [HttpGet]
        public async Task<IActionResult> EditLost(int id)
        {
            if (HttpContext.Session.GetString("UserType") != "Admin")
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            var report = await _db.LostPets
                .FirstOrDefaultAsync(x => x.LostID == id);

            if (report == null)
            {
                return NotFound();
            }

            ViewBag.Pets = await _db.Pets
                .OrderBy(p => p.PetName)
                .ToListAsync();

            return View(report);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLost(LostPet model)
        {
            if (HttpContext.Session.GetString("UserType") != "Admin")
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            var report = await _db.LostPets
                .FirstOrDefaultAsync(x => x.LostID == model.LostID);

            if (report == null)
            {
                return NotFound();
            }

            report.PetID = model.PetID;
            report.LostLocation = model.LostLocation;
            report.LostDate = model.LostDate;
            report.ContactNumber = model.ContactNumber;

            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // =========================
        // DELETE LOST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLost(int id)
        {
            if (HttpContext.Session.GetString("UserType") != "Admin")
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            var report = await _db.LostPets
                .FirstOrDefaultAsync(x => x.LostID == id);

            if (report == null)
            {
                return NotFound();
            }

            _db.LostPets.Remove(report);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // =========================
        // CREATE FOUND
        // =========================
        [HttpGet]
        public async Task<IActionResult> CreateFound()
        {
            if (HttpContext.Session.GetString("UserType") != "Admin")
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            ViewBag.Pets = await _db.Pets
                .OrderBy(p => p.PetName)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFound(FoundPet model)
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
                model.UserID = null;

                _db.FoundPets.Add(model);
                await _db.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            ViewBag.Pets = await _db.Pets
                .OrderBy(p => p.PetName)
                .ToListAsync();

            return View(model);
        }

        // =========================
        // EDIT FOUND
        // =========================
        [HttpGet]
        public async Task<IActionResult> EditFound(int id)
        {
            if (HttpContext.Session.GetString("UserType") != "Admin")
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            var report = await _db.FoundPets
                .FirstOrDefaultAsync(x => x.FoundID == id);

            if (report == null)
            {
                return NotFound();
            }

            ViewBag.Pets = await _db.Pets
                .OrderBy(p => p.PetName)
                .ToListAsync();

            return View(report);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFound(FoundPet model)
        {
            if (HttpContext.Session.GetString("UserType") != "Admin")
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            var report = await _db.FoundPets
                .FirstOrDefaultAsync(x => x.FoundID == model.FoundID);

            if (report == null)
            {
                return NotFound();
            }

            report.PetID = model.PetID;
            report.FoundLocation = model.FoundLocation;
            report.FoundDate = model.FoundDate;
            report.ReporterName = model.ReporterName;
            report.ContactNumber = model.ContactNumber;

            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // =========================
        // DELETE FOUND
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFound(int id)
        {
            if (HttpContext.Session.GetString("UserType") != "Admin")
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            var report = await _db.FoundPets
                .FirstOrDefaultAsync(x => x.FoundID == id);

            if (report == null)
            {
                return NotFound();
            }

            _db.FoundPets.Remove(report);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}