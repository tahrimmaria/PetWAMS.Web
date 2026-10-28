using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Models;

namespace PetWAMS.Web.Controllers
{
    public class PetCareShopVariantCartController : Controller
    {
        private readonly PetWamsDbContext _db;

        public PetCareShopVariantCartController(PetWamsDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(
            int productId,
            int? productVariantId)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction(
                    "SignIn",
                    "Account"
                );
            }

            var product = await _db.PetShopProducts
                .FirstOrDefaultAsync(p =>
                    p.ProductID == productId);

            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction("Index", "PetCareShop");
            }

            if (!product.IsActive ||
                product.ApprovalStatus == null ||
                !product.ApprovalStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "This product is not available for order.";
                return RedirectToAction("Index", "PetCareShop");
            }

            var activeVariants = await _db.PetShopProductVariants
                .Where(v =>
                    v.ProductID == productId &&
                    v.IsActive)
                .ToListAsync();

            PetShopProductVariant? selectedVariant = null;

            int stockLimit;

            if (activeVariants.Count > 0)
            {
                if (productVariantId == null || productVariantId <= 0)
                {
                    TempData["Error"] = "Please select a product color/variant first.";
                    return RedirectToAction("Index", "PetCareShop");
                }

                selectedVariant = activeVariants
                    .FirstOrDefault(v =>
                        v.ProductVariantID == productVariantId.Value);

                if (selectedVariant == null)
                {
                    TempData["Error"] = "Selected variant not found.";
                    return RedirectToAction("Index", "PetCareShop");
                }

                if (selectedVariant.StockQuantity <= 0)
                {
                    TempData["Error"] = "Selected variant is out of stock.";
                    return RedirectToAction("Index", "PetCareShop");
                }

                stockLimit = selectedVariant.StockQuantity;
            }
            else
            {
                productVariantId = null;

                if (product.StockQuantity <= 0)
                {
                    TempData["Error"] = "This product is out of stock.";
                    return RedirectToAction("Index", "PetCareShop");
                }

                stockLimit = product.StockQuantity;
            }

            var cartItem = await _db.PetShopCartItems
                .FirstOrDefaultAsync(c =>
                    c.UserID == userId.Value &&
                    c.ProductID == productId &&
                    c.ProductVariantID == productVariantId);

            if (cartItem == null)
            {
                cartItem = new PetShopCartItem
                {
                    UserID = userId.Value,
                    ProductID = productId,
                    ProductVariantID = productVariantId,
                    Quantity = 1,
                    CreatedAt = DateTime.Now
                };

                _db.PetShopCartItems.Add(cartItem);
            }
            else
            {
                if (cartItem.Quantity + 1 > stockLimit)
                {
                    TempData["Error"] = "You cannot add more than available stock.";
                    return RedirectToAction("Index", "PetCareShop");
                }

                cartItem.Quantity += 1;
            }

            await _db.SaveChangesAsync();

            if (selectedVariant != null)
            {
                TempData["Success"] =
                    $"{product.ProductName} - {selectedVariant.VariantName} added to cart.";
            }
            else
            {
                TempData["Success"] =
                    $"{product.ProductName} added to cart.";
            }

            return RedirectToAction("Index", "PetCareShop");
        }
    }
}