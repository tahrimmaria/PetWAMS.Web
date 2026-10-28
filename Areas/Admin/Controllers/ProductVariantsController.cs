using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Models;

namespace PetWAMS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductVariantsController : Controller
    {
        private readonly PetWamsDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ProductVariantsController(
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

            var variants = await _db.PetShopProductVariants
                .AsNoTracking()
                .Where(v => productIds.Contains(v.ProductID))
                .ToListAsync();

            ViewBag.VariantCounts = variants
                .GroupBy(v => v.ProductID)
                .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.ActiveVariantCounts = variants
                .Where(v => v.IsActive)
                .GroupBy(v => v.ProductID)
                .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.TotalVariantStocks = variants
                .Where(v => v.IsActive)
                .GroupBy(v => v.ProductID)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.StockQuantity));

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

            var variants = await _db.PetShopProductVariants
                .AsNoTracking()
                .Where(v => v.ProductID == id)
                .OrderByDescending(v => v.IsActive)
                .ThenBy(v => v.VariantName)
                .ToListAsync();

            ViewBag.Variants = variants;

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(
            int productId,
            string variantName,
            string? colorCode,
            int stockQuantity,
            IFormFile image)
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

            if (string.IsNullOrWhiteSpace(variantName))
            {
                TempData["Error"] = "Variant name is required.";
                return RedirectToAction("Manage", new { id = productId });
            }

            if (stockQuantity < 0)
            {
                TempData["Error"] = "Stock cannot be negative.";
                return RedirectToAction("Manage", new { id = productId });
            }

            bool duplicate = await _db.PetShopProductVariants
                .AnyAsync(v =>
                    v.ProductID == productId &&
                    v.VariantName.ToLower() == variantName.Trim().ToLower());

            if (duplicate)
            {
                TempData["Error"] = "This variant already exists for this product.";
                return RedirectToAction("Manage", new { id = productId });
            }

            if (image == null || image.Length == 0)
            {
                TempData["Error"] = "Variant image is required.";
                return RedirectToAction("Manage", new { id = productId });
            }

            string? imagePath = await SaveVariantImageAsync(image, productId);

            if (string.IsNullOrWhiteSpace(imagePath))
            {
                TempData["Error"] = "Invalid image format. Use jpg, jpeg, png or webp.";
                return RedirectToAction("Manage", new { id = productId });
            }

            var variant = new PetShopProductVariant
            {
                ProductID = productId,
                VariantName = variantName.Trim(),
                ColorCode = string.IsNullOrWhiteSpace(colorCode) ? null : colorCode.Trim(),
                ImagePath = imagePath,
                StockQuantity = stockQuantity,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _db.PetShopProductVariants.Add(variant);

            await _db.SaveChangesAsync();

            await SyncProductStockAndImageAsync(productId);

            TempData["Success"] = "Product variant added successfully.";
            return RedirectToAction("Manage", new { id = productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(
            int variantId,
            string variantName,
            string? colorCode,
            int stockQuantity,
            IFormFile? image)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }

            var variant = await _db.PetShopProductVariants
                .FirstOrDefaultAsync(v => v.ProductVariantID == variantId);

            if (variant == null)
            {
                TempData["Error"] = "Variant not found.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(variantName))
            {
                TempData["Error"] = "Variant name is required.";
                return RedirectToAction("Manage", new { id = variant.ProductID });
            }

            if (stockQuantity < 0)
            {
                TempData["Error"] = "Stock cannot be negative.";
                return RedirectToAction("Manage", new { id = variant.ProductID });
            }

            bool duplicate = await _db.PetShopProductVariants
                .AnyAsync(v =>
                    v.ProductID == variant.ProductID &&
                    v.ProductVariantID != variantId &&
                    v.VariantName.ToLower() == variantName.Trim().ToLower());

            if (duplicate)
            {
                TempData["Error"] = "Another variant with this name already exists.";
                return RedirectToAction("Manage", new { id = variant.ProductID });
            }

            variant.VariantName = variantName.Trim();
            variant.ColorCode = string.IsNullOrWhiteSpace(colorCode) ? null : colorCode.Trim();
            variant.StockQuantity = stockQuantity;

            if (image != null && image.Length > 0)
            {
                string? imagePath = await SaveVariantImageAsync(image, variant.ProductID);

                if (string.IsNullOrWhiteSpace(imagePath))
                {
                    TempData["Error"] = "Invalid image format. Use jpg, jpeg, png or webp.";
                    return RedirectToAction("Manage", new { id = variant.ProductID });
                }

                DeletePhysicalFile(variant.ImagePath);

                variant.ImagePath = imagePath;
            }

            await _db.SaveChangesAsync();

            await SyncProductStockAndImageAsync(variant.ProductID);

            TempData["Success"] = "Variant updated successfully.";
            return RedirectToAction("Manage", new { id = variant.ProductID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int variantId)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }

            var variant = await _db.PetShopProductVariants
                .FirstOrDefaultAsync(v => v.ProductVariantID == variantId);

            if (variant == null)
            {
                TempData["Error"] = "Variant not found.";
                return RedirectToAction("Index");
            }

            variant.IsActive = !variant.IsActive;

            await _db.SaveChangesAsync();

            await SyncProductStockAndImageAsync(variant.ProductID);

            TempData["Success"] = "Variant status updated.";
            return RedirectToAction("Manage", new { id = variant.ProductID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int variantId)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }

            var variant = await _db.PetShopProductVariants
                .FirstOrDefaultAsync(v => v.ProductVariantID == variantId);

            if (variant == null)
            {
                TempData["Error"] = "Variant not found.";
                return RedirectToAction("Index");
            }

            int productId = variant.ProductID;

            bool usedInCart = await _db.PetShopCartItems
                .AnyAsync(c => c.ProductVariantID == variantId);

            bool usedInOrder = await _db.PetShopOrderItems
                .AnyAsync(i => i.ProductVariantID == variantId);

            if (usedInCart || usedInOrder)
            {
                variant.IsActive = false;

                await _db.SaveChangesAsync();

                await SyncProductStockAndImageAsync(productId);

                TempData["Success"] =
                    "Variant already used in cart/order, so it was deactivated instead of deleted.";

                return RedirectToAction("Manage", new { id = productId });
            }

            DeletePhysicalFile(variant.ImagePath);

            _db.PetShopProductVariants.Remove(variant);

            await _db.SaveChangesAsync();

            await SyncProductStockAndImageAsync(productId);

            TempData["Success"] = "Variant deleted successfully.";
            return RedirectToAction("Manage", new { id = productId });
        }

        private async Task<string?> SaveVariantImageAsync(
            IFormFile image,
            int productId)
        {
            string extension = Path
                .GetExtension(image.FileName)
                .ToLowerInvariant();

            bool allowed =
                extension == ".jpg" ||
                extension == ".jpeg" ||
                extension == ".png" ||
                extension == ".webp";

            if (!allowed)
            {
                return null;
            }

            string wwwRootPath = _env.WebRootPath
                ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            string uploadFolder = Path.Combine(
                wwwRootPath,
                "images",
                "product-variants"
            );

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            string fileName =
                "variant_" +
                productId +
                "_" +
                Guid.NewGuid().ToString("N") +
                extension;

            string fullPath = Path.Combine(uploadFolder, fileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            return "/images/product-variants/" + fileName;
        }

        private void DeletePhysicalFile(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return;
            }

            string wwwRootPath = _env.WebRootPath
                ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            string cleanPath = relativePath.TrimStart('/');

            string fullPath = Path.Combine(
                wwwRootPath,
                cleanPath.Replace("/", Path.DirectorySeparatorChar.ToString())
            );

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }

        private async Task SyncProductStockAndImageAsync(int productId)
        {
            var product = await _db.PetShopProducts
                .FirstOrDefaultAsync(p => p.ProductID == productId);

            if (product == null)
            {
                return;
            }

            var activeVariants = await _db.PetShopProductVariants
                .Where(v =>
                    v.ProductID == productId &&
                    v.IsActive)
                .OrderBy(v => v.VariantName)
                .ToListAsync();

            if (activeVariants.Count == 0)
            {
                await _db.SaveChangesAsync();
                return;
            }

            product.StockQuantity = activeVariants.Sum(v => v.StockQuantity);

            var firstImageVariant = activeVariants
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v.ImagePath));

            if (firstImageVariant != null)
            {
                product.ImagePath = firstImageVariant.ImagePath;
            }

            await _db.SaveChangesAsync();
        }
    }
}