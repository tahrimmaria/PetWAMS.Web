using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Models;

namespace PetWAMS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PetImagesController : Controller
    {
        private readonly PetWamsDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PetImagesController(
            PetWamsDbContext db,
            IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetInt32("UserID") != null &&
                   HttpContext.Session.GetString("UserType") == "Admin";
        }

        // =========================
        // ALL PETS IMAGE MANAGER
        // =========================
        public async Task<IActionResult> Index(string? search)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new { area = "" }
                );
            }

            var query = _db.Pets
                .Include(p => p.Category)
                .Include(p => p.Shelter)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(p =>
                    p.PetName.Contains(search) ||
                    p.Breed.Contains(search) ||
                    (
                        p.Category != null &&
                        p.Category.CategoryName.Contains(search)
                    ) ||
                    (
                        p.Shelter != null &&
                        p.Shelter.ShelterName.Contains(search)
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
            ViewBag.Search = search ?? "";

            return View(pets);
        }

        // =========================
        // MANAGE SINGLE PET IMAGES
        // =========================
        public async Task<IActionResult> Manage(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new { area = "" }
                );
            }

            var pet = await _db.Pets
                .Include(p => p.Category)
                .Include(p => p.Shelter)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PetID == id);

            if (pet == null)
            {
                return NotFound();
            }

            var images = await _db.PetImages
                .Where(x => x.PetID == id)
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.PetImageID)
                .ToListAsync();

            ViewBag.Pet = pet;

            return View(images);
        }

        // =========================
        // UPLOAD MULTIPLE IMAGES
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(
            int petId,
            List<IFormFile>? petImages)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new { area = "" }
                );
            }

            bool petExists = await _db.Pets
                .AnyAsync(p => p.PetID == petId);

            if (!petExists)
            {
                return NotFound();
            }

            await SavePetImagesAsync(
                petId,
                petImages
            );

            TempData["Success"] = "Pet images uploaded successfully.";

            return RedirectToAction(
                "Manage",
                new { id = petId }
            );
        }

        // =========================
        // SET PRIMARY IMAGE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPrimaryImage(
            int petId,
            int imageId)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new { area = "" }
                );
            }

            bool petExists = await _db.Pets
                .AnyAsync(p => p.PetID == petId);

            if (!petExists)
            {
                return NotFound();
            }

            var images = await _db.PetImages
                .Where(x => x.PetID == petId)
                .ToListAsync();

            if (!images.Any(x => x.PetImageID == imageId))
            {
                return NotFound();
            }

            foreach (var image in images)
            {
                image.IsPrimary = image.PetImageID == imageId;
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = "Primary image updated.";

            return RedirectToAction(
                "Manage",
                new { id = petId }
            );
        }

        // =========================
        // DELETE IMAGE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(
            int petId,
            int imageId)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new { area = "" }
                );
            }

            var image = await _db.PetImages
                .FirstOrDefaultAsync(x =>
                    x.PetID == petId &&
                    x.PetImageID == imageId);

            if (image == null)
            {
                return RedirectToAction(
                    "Manage",
                    new { id = petId }
                );
            }

            bool wasPrimary = image.IsPrimary;

            DeleteFileFromWwwRoot(image.ImagePath);

            _db.PetImages.Remove(image);
            await _db.SaveChangesAsync();

            if (wasPrimary)
            {
                var nextImage = await _db.PetImages
                    .Where(x => x.PetID == petId)
                    .OrderBy(x => x.PetImageID)
                    .FirstOrDefaultAsync();

                if (nextImage != null)
                {
                    nextImage.IsPrimary = true;
                    await _db.SaveChangesAsync();
                }
            }

            TempData["Success"] = "Image deleted successfully.";

            return RedirectToAction(
                "Manage",
                new { id = petId }
            );
        }

        // =========================
        // SAVE PET IMAGES
        // =========================
        private async Task SavePetImagesAsync(
            int petId,
            List<IFormFile>? petImages)
        {
            if (petImages == null || petImages.Count == 0)
            {
                return;
            }

            string uploadFolder = Path.Combine(
                _webHostEnvironment.WebRootPath,
                "images",
                "pets"
            );

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            bool hasPrimaryImage = await _db.PetImages
                .AnyAsync(x =>
                    x.PetID == petId &&
                    x.IsPrimary);

            int savedCount = 0;

            foreach (var imageFile in petImages)
            {
                if (imageFile == null || imageFile.Length == 0)
                {
                    continue;
                }

                string extension = Path
                    .GetExtension(imageFile.FileName)
                    .ToLower();

                bool allowed =
                    extension == ".jpg" ||
                    extension == ".jpeg" ||
                    extension == ".png" ||
                    extension == ".webp";

                if (!allowed)
                {
                    continue;
                }

                string fileName =
                    "pet_" +
                    petId +
                    "_" +
                    Guid.NewGuid().ToString("N") +
                    extension;

                string filePath = Path.Combine(
                    uploadFolder,
                    fileName
                );

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                string relativePath = "/images/pets/" + fileName;

                var petImage = new PetImage
                {
                    PetID = petId,
                    ImagePath = relativePath,
                    IsPrimary = !hasPrimaryImage && savedCount == 0,
                    UploadedAt = DateTime.Now
                };

                _db.PetImages.Add(petImage);

                savedCount++;
            }

            if (savedCount > 0)
            {
                await _db.SaveChangesAsync();
            }
        }

        // =========================
        // DELETE PHYSICAL FILE
        // =========================
        private void DeleteFileFromWwwRoot(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return;
            }

            string cleanPath = relativePath.TrimStart('/');

            string fullPath = Path.Combine(
                _webHostEnvironment.WebRootPath,
                cleanPath.Replace("/", Path.DirectorySeparatorChar.ToString())
            );

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}