using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Models;

namespace PetWAMS.Web.Areas.Shelter.Controllers
{
    [Area("Shelter")]
    public class PetsController : Controller
    {
        private readonly PetWamsDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PetsController(
            PetWamsDbContext db,
            IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }

        private bool IsLoggedInStaff()
        {
            return HttpContext.Session.GetInt32("UserID") != null &&
                   HttpContext.Session.GetString("UserType") == "Staff";
        }

        private async Task<int?> GetStaffShelterId()
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return null;
            }

            return await _db.Users
                .Where(u => u.UserID == userId.Value)
                .Select(u => u.ShelterID)
                .FirstOrDefaultAsync();
        }


        // =========================
        // MANAGE PETS
        // =========================
        public async Task<IActionResult> Index()
        {
            if (!IsLoggedInStaff())
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

            ViewBag.FullName =
                HttpContext.Session.GetString("FullName");

            var pets = await _db.Pets
                .Include(p => p.Category)
                .Include(p => p.Shelter)
                .Where(p => p.ShelterID == shelterId.Value)
                .OrderBy(p => p.PetID)
                .ToListAsync();

            var petIds = pets.Select(p => p.PetID).ToList();

            var images = await _db.PetImages
                .AsNoTracking()
                .Where(x => petIds.Contains(x.PetID))
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.PetImageID)
                .ToListAsync();

            ViewBag.PetImages = images;

            return View(pets);
        }


        // =========================
        // CREATE PET
        // =========================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!IsLoggedInStaff())
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

            ViewBag.Shelters = await _db.Shelters
                .Where(s => s.ShelterID == shelterId.Value)
                .ToListAsync();

            ViewBag.Categories = await _db.PetCategories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            Pet pet,
            List<IFormFile>? petImages)
        {
            if (!IsLoggedInStaff())
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

            // Never trust ShelterID from form
            pet.ShelterID = shelterId.Value;

            pet.ArrivalDate = DateTime.Now;
            pet.Status = "Available";

            _db.Pets.Add(pet);
            await _db.SaveChangesAsync();

            await SavePetImagesAsync(
                pet.PetID,
                petImages
            );

            return RedirectToAction("Index");
        }


        // =========================
        // EDIT PET
        // =========================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsLoggedInStaff())
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

            var pet = await _db.Pets
                .FirstOrDefaultAsync(p =>
                    p.PetID == id &&
                    p.ShelterID == shelterId.Value);

            if (pet == null)
            {
                return NotFound();
            }

            ViewBag.Categories = await _db.PetCategories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            ViewBag.PetImages = await _db.PetImages
                .Where(x => x.PetID == id)
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.PetImageID)
                .ToListAsync();

            return View(pet);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(
            int id,
            Pet updated,
            List<IFormFile>? petImages)
        {
            if (!IsLoggedInStaff())
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

            var pet = await _db.Pets
                .FirstOrDefaultAsync(p =>
                    p.PetID == id &&
                    p.ShelterID == shelterId.Value);

            if (pet == null)
            {
                return NotFound();
            }

            pet.PetName = updated.PetName;
            pet.CategoryID = updated.CategoryID;
            pet.Breed = updated.Breed;
            pet.Age = updated.Age;
            pet.Gender = updated.Gender;
            pet.Color = updated.Color;
            pet.Status = updated.Status;
            pet.Description = updated.Description;

            // ShelterID change করা যাবে না
            pet.ShelterID = shelterId.Value;

            await _db.SaveChangesAsync();

            await SavePetImagesAsync(
                pet.PetID,
                petImages
            );

            return RedirectToAction("Index");
        }


        // =========================
        // SET PRIMARY PET IMAGE
        // =========================
        [HttpPost]
        public async Task<IActionResult> SetPrimaryImage(
            int petId,
            int imageId)
        {
            if (!IsLoggedInStaff())
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

            bool petBelongsToShelter = await _db.Pets
                .AnyAsync(p =>
                    p.PetID == petId &&
                    p.ShelterID == shelterId.Value);

            if (!petBelongsToShelter)
            {
                return NotFound();
            }

            var images = await _db.PetImages
                .Where(x => x.PetID == petId)
                .ToListAsync();

            foreach (var image in images)
            {
                image.IsPrimary = image.PetImageID == imageId;
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = "Primary image updated.";

            return RedirectToAction("Edit", new { id = petId });
        }


        // =========================
        // DELETE PET IMAGE
        // =========================
        [HttpPost]
        public async Task<IActionResult> DeleteImage(
            int petId,
            int imageId)
        {
            if (!IsLoggedInStaff())
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

            bool petBelongsToShelter = await _db.Pets
                .AnyAsync(p =>
                    p.PetID == petId &&
                    p.ShelterID == shelterId.Value);

            if (!petBelongsToShelter)
            {
                return NotFound();
            }

            var image = await _db.PetImages
                .FirstOrDefaultAsync(x =>
                    x.PetImageID == imageId &&
                    x.PetID == petId);

            if (image == null)
            {
                return RedirectToAction("Edit", new { id = petId });
            }

            DeleteFileFromWwwRoot(image.ImagePath);

            bool wasPrimary = image.IsPrimary;

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

            TempData["Success"] = "Image deleted.";

            return RedirectToAction("Edit", new { id = petId });
        }


        // =========================
        // DELETE PET
        // =========================
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsLoggedInStaff())
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

            var pet = await _db.Pets
                .FirstOrDefaultAsync(p =>
                    p.PetID == id &&
                    p.ShelterID == shelterId.Value);

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

            var petImages = await _db.PetImages
                .Where(x => x.PetID == id)
                .ToListAsync();

            foreach (var image in petImages)
            {
                DeleteFileFromWwwRoot(image.ImagePath);
            }

            _db.Pets.Remove(pet);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }


        // =========================
        // SAVE MULTIPLE PET IMAGES
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

                string extension = Path.GetExtension(imageFile.FileName).ToLower();

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
        // DELETE FILE FROM WWWROOT
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