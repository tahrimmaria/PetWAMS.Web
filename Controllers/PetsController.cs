using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;

namespace PetWAMS.Web.Controllers
{
    public class PetsController : Controller
    {
        private readonly PetWamsDbContext _db;

        public PetsController(PetWamsDbContext db)
        {
            _db = db;
        }

        // =========================
        // PUBLIC AVAILABLE PET LIST
        // =========================
        public async Task<IActionResult> Index(int? categoryId, string? search)
        {
            var categories = await _db.PetCategories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            var selectedCategory = categoryId.HasValue
                ? categories.FirstOrDefault(c => c.CategoryID == categoryId.Value)
                : null;

            string selectedCategoryName = selectedCategory?.CategoryName ?? "";

            var query = _db.Pets
                .Include(p => p.Category)
                .Include(p => p.Shelter)
                .Include(p => p.HealthRecords)
                .Where(p =>
                    p.Status != null &&
                    p.Status.Trim().ToLower() == "available" &&

                    !_db.Adoptions.Any(a =>
                        a.PetID == p.PetID) &&

                    !_db.AdoptionRequests.Any(r =>
                        r.PetID == p.PetID &&
                        r.Status != null &&
                        r.Status.Trim().ToLower() == "approved"))
                .AsQueryable();

            // Bird select korle Bird + Parrot duita show korbe
            // Parrot er jonno alada button lagbe na
            if (categoryId.HasValue && selectedCategory != null)
            {
                if (selectedCategoryName.Equals("Bird", StringComparison.OrdinalIgnoreCase) ||
                    selectedCategoryName.Equals("Parrot", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(p =>
                        p.Category != null &&
                        (
                            p.Category.CategoryName == "Bird" ||
                            p.Category.CategoryName == "Parrot"
                        )
                    );
                }
                else
                {
                    query = query.Where(p =>
                        p.CategoryID == categoryId.Value);
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(p =>
                    p.PetName.Contains(search) ||
                    (
                        p.Breed != null &&
                        p.Breed.Contains(search)
                    ) ||
                    (
                        p.Category != null &&
                        p.Category.CategoryName.Contains(search)
                    )
                );
            }

            var pets = await query
                .OrderBy(p => p.PetName)
                .ToListAsync();

            var petIds = pets
                .Select(p => p.PetID)
                .ToList();

            var petImages = await _db.PetImages
                .AsNoTracking()
                .Where(x => petIds.Contains(x.PetID))
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.PetImageID)
                .ToListAsync();

            ViewBag.PetImages = petImages;

            ViewBag.Categories = categories;

            ViewBag.SelectedCategoryId = categoryId;

            if (selectedCategoryName.Equals("Parrot", StringComparison.OrdinalIgnoreCase))
            {
                selectedCategoryName = "Bird";
            }

            ViewBag.SelectedCategoryName = selectedCategoryName;

            ViewBag.Search = search ?? "";

            return View(pets);
        }

        // =========================
        // PUBLIC AVAILABLE PET DETAILS
        // =========================
        public async Task<IActionResult> Details(int id)
        {
            var pet = await _db.Pets
                .Include(p => p.Category)
                .Include(p => p.Shelter)
                .Include(p => p.HealthRecords)
                .FirstOrDefaultAsync(p =>
                    p.PetID == id &&

                    p.Status != null &&
                    p.Status.Trim().ToLower() == "available" &&

                    !_db.Adoptions.Any(a =>
                        a.PetID == p.PetID) &&

                    !_db.AdoptionRequests.Any(r =>
                        r.PetID == p.PetID &&
                        r.Status != null &&
                        r.Status.Trim().ToLower() == "approved"));

            if (pet == null)
            {
                return NotFound();
            }

            var petImages = await _db.PetImages
                .AsNoTracking()
                .Where(x => x.PetID == id)
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.PetImageID)
                .ToListAsync();

            ViewBag.PetImages = petImages;

            return View(pet);
        }
    }
}