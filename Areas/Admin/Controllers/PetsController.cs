using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Models;

namespace PetWAMS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PetsController : Controller
    {
        private readonly PetWamsDbContext _db;

        public PetsController(PetWamsDbContext db)
        {
            _db = db;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("UserType") == "Admin";
        }


        // =========================
        // PET LIST
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

            var pets = await _db.Pets
                .Include(p => p.Category)
                .Include(p => p.Shelter)
                .OrderBy(p => p.PetID)
                .ToListAsync();

            return View(pets);
        }


        // =========================
        // CREATE PET
        // =========================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            ViewBag.Shelters = await _db.Shelters
                .OrderBy(s => s.ShelterName)
                .ToListAsync();

            ViewBag.Categories = await _db.PetCategories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Pet pet)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            if (pet.ArrivalDate == default)
            {
                pet.ArrivalDate = DateTime.Now;
            }

            if (string.IsNullOrWhiteSpace(pet.Status))
            {
                pet.Status = "Available";
            }

            if (ModelState.IsValid)
            {
                _db.Pets.Add(pet);
                await _db.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            ViewBag.Shelters = await _db.Shelters
                .OrderBy(s => s.ShelterName)
                .ToListAsync();

            ViewBag.Categories = await _db.PetCategories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            return View(pet);
        }


        // =========================
        // EDIT PET
        // =========================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            var pet = await _db.Pets
                .FirstOrDefaultAsync(p => p.PetID == id);

            if (pet == null)
            {
                return NotFound();
            }

            ViewBag.Shelters = await _db.Shelters
                .OrderBy(s => s.ShelterName)
                .ToListAsync();

            ViewBag.Categories = await _db.PetCategories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            return View(pet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Pet updated)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            var pet = await _db.Pets
                .FirstOrDefaultAsync(p =>
                    p.PetID == updated.PetID);

            if (pet == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Shelters = await _db.Shelters
                    .OrderBy(s => s.ShelterName)
                    .ToListAsync();

                ViewBag.Categories = await _db.PetCategories
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();

                return View(updated);
            }

            pet.PetName = updated.PetName;
            pet.Breed = updated.Breed;
            pet.Age = updated.Age;
            pet.Gender = updated.Gender;
            pet.Color = updated.Color;
            pet.Status = updated.Status;

            pet.ShelterID = updated.ShelterID;
            pet.CategoryID = updated.CategoryID;

            pet.MicrochipNumber = updated.MicrochipNumber;
            pet.ArrivalDate = updated.ArrivalDate;

            pet.AIRecommendation = updated.AIRecommendation;
            pet.Description = updated.Description;

            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }


        // =========================
        // DELETE PET
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            var pet = await _db.Pets
                .FirstOrDefaultAsync(p => p.PetID == id);

            if (pet == null)
            {
                return NotFound();
            }

            bool hasRelatedData =
                await _db.AdoptionRequests.AnyAsync(x => x.PetID == id) ||
                await _db.Adoptions.AnyAsync(x => x.PetID == id) ||
                await _db.HealthRecords.AnyAsync(x => x.PetID == id) ||
                await _db.LostPets.AnyAsync(x => x.PetID == id) ||
                await _db.FoundPets.AnyAsync(x => x.PetID == id);

            if (hasRelatedData)
            {
                TempData["Error"] =
                    "This pet cannot be deleted because related records exist.";

                return RedirectToAction("Index");
            }

            _db.Pets.Remove(pet);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}