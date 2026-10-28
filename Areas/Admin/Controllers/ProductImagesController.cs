using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Models;

namespace PetWAMS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductImagesController : Controller
    {
        private readonly PetWamsDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ProductImagesController(
            PetWamsDbContext db,
            IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        private bool IsAdminLoggedIn()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            string? userType = HttpContext.Session.GetString("UserType");

            return userId != null &&
                   !string.IsNullOrWhiteSpace(userType) &&
                   userType.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<IActionResult> Index()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }

            var products = await _db.PetShopProducts
                .AsNoTracking()
                .OrderBy(p => p.ProductCategory)
                .ThenBy(p => p.ProductName)
                .ToListAsync();

            var productIds = products
                .Select(p => p.ProductID)
                .ToList();

            var images = await _db.PetShopProductImages
                .AsNoTracking()
                .Where(x => productIds.Contains(x.ProductID))
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.ProductImageID)
                .ToListAsync();

            ViewBag.ImageCounts = images
                .GroupBy(x => x.ProductID)
                .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.PrimaryImages = images
                .GroupBy(x => x.ProductID)
                .ToDictionary(
                    g => g.Key,
                    g => g
                        .OrderByDescending(x => x.IsPrimary)
                        .ThenBy(x => x.ProductImageID)
                        .FirstOrDefault()?.ImagePath ?? ""
                );

            return View(products);
        }

        public async Task<IActionResult> Manage(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }

            var product = await _db.PetShopProducts
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null)
            {
                return NotFound();
            }

            var images = await _db.PetShopProductImages
                .AsNoTracking()
                .Where(x => x.ProductID == id)
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.ProductImageID)
                .ToListAsync();

            ViewBag.ProductImages = images;

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(
            int productId,
            List<IFormFile> images)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }

            var product = await _db.PetShopProducts
                .FirstOrDefaultAsync(p => p.ProductID == productId);

            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction("Index");
            }

            if (images == null || images.Count == 0)
            {
                TempData["Error"] = "Please select at least one image.";
                return RedirectToAction("Manage", new { id = productId });
            }

            string wwwRootPath = _env.WebRootPath
                ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            string uploadFolder = Path.Combine(
                wwwRootPath,
                "images",
                "products"
            );

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            bool hasPrimaryImage = await _db.PetShopProductImages
                .AnyAsync(x =>
                    x.ProductID == productId &&
                    x.IsPrimary);

            int uploadedCount = 0;

            foreach (var image in images)
            {
                if (image == null || image.Length == 0)
                {
                    continue;
                }

                string extension = Path
                    .GetExtension(image.FileName)
                    .ToLowerInvariant();

                bool isAllowed =
                    extension == ".jpg" ||
                    extension == ".jpeg" ||
                    extension == ".png" ||
                    extension == ".webp";

                if (!isAllowed)
                {
                    continue;
                }

                string fileName =
                    "product_" +
                    productId +
                    "_" +
                    Guid.NewGuid().ToString("N") +
                    extension;

                string fullPath = Path.Combine(uploadFolder, fileName);

                await using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                string relativePath = "/images/products/" + fileName;

                bool makePrimary =
                    !hasPrimaryImage &&
                    uploadedCount == 0;

                var productImage = new PetShopProductImage
                {
                    ProductID = productId,
                    ImagePath = relativePath,
                    IsPrimary = makePrimary,
                    UploadedAt = DateTime.Now
                };

                _db.PetShopProductImages.Add(productImage);

                if (makePrimary)
                {
                    product.ImagePath = relativePath;
                    hasPrimaryImage = true;
                }

                uploadedCount++;
            }

            if (uploadedCount == 0)
            {
                TempData["Error"] = "No valid image uploaded. Use jpg, jpeg, png or webp.";
                return RedirectToAction("Manage", new { id = productId });
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = uploadedCount + " product image(s) uploaded successfully.";
            return RedirectToAction("Manage", new { id = productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPrimary(int imageId)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }

            var selectedImage = await _db.PetShopProductImages
                .FirstOrDefaultAsync(x => x.ProductImageID == imageId);

            if (selectedImage == null)
            {
                TempData["Error"] = "Image not found.";
                return RedirectToAction("Index");
            }

            var product = await _db.PetShopProducts
                .FirstOrDefaultAsync(p => p.ProductID == selectedImage.ProductID);

            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction("Index");
            }

            var allImages = await _db.PetShopProductImages
                .Where(x => x.ProductID == selectedImage.ProductID)
                .ToListAsync();

            foreach (var image in allImages)
            {
                image.IsPrimary = false;
            }

            selectedImage.IsPrimary = true;
            product.ImagePath = selectedImage.ImagePath;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Primary product image updated.";
            return RedirectToAction("Manage", new { id = selectedImage.ProductID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int imageId)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }

            var image = await _db.PetShopProductImages
                .FirstOrDefaultAsync(x => x.ProductImageID == imageId);

            if (image == null)
            {
                TempData["Error"] = "Image not found.";
                return RedirectToAction("Index");
            }

            int productId = image.ProductID;
            bool wasPrimary = image.IsPrimary;
            string deletedImagePath = image.ImagePath;

            var product = await _db.PetShopProducts
                .FirstOrDefaultAsync(p => p.ProductID == productId);

            string wwwRootPath = _env.WebRootPath
                ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            string relativePath = deletedImagePath.TrimStart('/');

            string fullPath = Path.Combine(
                wwwRootPath,
                relativePath.Replace("/", Path.DirectorySeparatorChar.ToString())
            );

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }

            _db.PetShopProductImages.Remove(image);
            await _db.SaveChangesAsync();

            var remainingImages = await _db.PetShopProductImages
                .Where(x => x.ProductID == productId)
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.ProductImageID)
                .ToListAsync();

            if (product != null)
            {
                if (remainingImages.Count == 0)
                {
                    product.ImagePath = null;
                }
                else if (wasPrimary || product.ImagePath == deletedImagePath)
                {
                    foreach (var item in remainingImages)
                    {
                        item.IsPrimary = false;
                    }

                    var newPrimary = remainingImages.First();
                    newPrimary.IsPrimary = true;
                    product.ImagePath = newPrimary.ImagePath;
                }

                await _db.SaveChangesAsync();
            }

            TempData["Success"] = "Product image deleted.";
            return RedirectToAction("Manage", new { id = productId });
        }
    }
}