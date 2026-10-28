using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Models;

namespace PetWAMS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PetCategoriesController : Controller
    {
        private readonly PetWamsDbContext _db;

        public PetCategoriesController(PetWamsDbContext db)
        {
            _db = db;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("UserType") == "Admin";
        }


        // =========================
        // CATEGORY LIST
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

            var categories = await _db.PetCategories
                .OrderBy(c => c.CategoryID)
                .ToListAsync();

            return View(categories);
        }


        // =========================
        // CREATE
        // =========================
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsAdmin())
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
        public async Task<IActionResult> Create(PetCategory model)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            if (string.IsNullOrWhiteSpace(model.CategoryName))
            {
                ModelState.AddModelError(
                    "CategoryName",
                    "Category name is required."
                );
            }

            bool exists = await _db.PetCategories
                .AnyAsync(c =>
                    c.CategoryName.ToLower() ==
                    model.CategoryName.ToLower());

            if (exists)
            {
                ModelState.AddModelError(
                    "CategoryName",
                    "This category already exists."
                );
            }

            if (ModelState.IsValid)
            {
                model.CategoryName = model.CategoryName.Trim();

                _db.PetCategories.Add(model);
                await _db.SaveChangesAsync();

                TempData["Success"] =
                    "Category added successfully.";

                return RedirectToAction("Index");
            }

            return View(model);
        }


        // =========================
        // EDIT
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

            var category = await _db.PetCategories
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PetCategory model)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            var category = await _db.PetCategories
                .FirstOrDefaultAsync(c =>
                    c.CategoryID == model.CategoryID);

            if (category == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(model.CategoryName))
            {
                ModelState.AddModelError(
                    "CategoryName",
                    "Category name is required."
                );
            }

            bool exists = await _db.PetCategories
                .AnyAsync(c =>
                    c.CategoryID != model.CategoryID &&
                    c.CategoryName.ToLower() ==
                    model.CategoryName.ToLower());

            if (exists)
            {
                ModelState.AddModelError(
                    "CategoryName",
                    "Another category with this name already exists."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            category.CategoryName =
                model.CategoryName.Trim();

            await _db.SaveChangesAsync();

            TempData["Success"] =
                "Category updated successfully.";

            return RedirectToAction("Index");
        }


        // =========================
        // DELETE
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

            var category = await _db.PetCategories
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
            {
                return NotFound();
            }

            bool hasPets = await _db.Pets
                .AnyAsync(p => p.CategoryID == id);

            if (hasPets)
            {
                TempData["Error"] =
                    "This category cannot be deleted because pets are using it.";

                return RedirectToAction("Index");
            }

            _db.PetCategories.Remove(category);
            await _db.SaveChangesAsync();

            TempData["Success"] =
                "Category deleted successfully.";

            return RedirectToAction("Index");
        }
    }
}
