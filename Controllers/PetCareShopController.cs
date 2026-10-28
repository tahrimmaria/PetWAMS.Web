using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Models;
using PetWAMS.Web.Services;

namespace PetWAMS.Web.Controllers
{
    public class PetCareShopController : Controller
    {
        private readonly PetWamsDbContext _db;
        private readonly SslCommerzService _sslCommerzService;

        public PetCareShopController(
            PetWamsDbContext db,
            SslCommerzService sslCommerzService)
        {
            _db = db;
            _sslCommerzService = sslCommerzService;
        }

        public async Task<IActionResult> Index(string? petType, string? category)
        {
            await SeedProductsIfEmptyAsync();

            var productIdsWithAvailableVariants = await _db.PetShopProductVariants
                .AsNoTracking()
                .Where(v => v.IsActive && v.StockQuantity > 0)
                .Select(v => v.ProductID)
                .Distinct()
                .ToListAsync();

            var productsQuery = _db.PetShopProducts
                .AsNoTracking()
                .Where(p =>
                    p.IsActive &&
                    p.ApprovalStatus == "Approved" &&
                    (
                        p.StockQuantity > 0 ||
                        productIdsWithAvailableVariants.Contains(p.ProductID)
                    ));

            if (!string.IsNullOrWhiteSpace(petType))
            {
                productsQuery = productsQuery.Where(p => p.PetType == petType);
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                productsQuery = productsQuery.Where(p => p.ProductCategory == category);
            }

            var products = await productsQuery
                .OrderBy(p => p.ProductCategory)
                .ThenBy(p => p.ProductName)
                .ToListAsync();

            var productIds = products.Select(p => p.ProductID).ToList();

            var productImages = await _db.PetShopProductImages
                .AsNoTracking()
                .Where(i => productIds.Contains(i.ProductID))
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.ProductImageID)
                .ToListAsync();

            var productVariants = await _db.PetShopProductVariants
                .AsNoTracking()
                .Where(v => productIds.Contains(v.ProductID) && v.IsActive)
                .OrderBy(v => v.VariantName)
                .ToListAsync();

            ViewBag.ProductImages = productImages
                .GroupBy(i => i.ProductID)
                .ToDictionary(g => g.Key, g => g.ToList());

            ViewBag.ProductVariants = productVariants
                .GroupBy(v => v.ProductID)
                .ToDictionary(g => g.Key, g => g.ToList());

            ViewBag.PetType = petType ?? "";
            ViewBag.Category = category ?? "";
            ViewBag.CartCount = await GetCartCountAsync();

            return View(products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int? productVariantId)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("SignIn", "Account");
            }

            var product = await _db.PetShopProducts
                .FirstOrDefaultAsync(p =>
                    p.ProductID == productId &&
                    p.IsActive &&
                    p.ApprovalStatus == "Approved");

            if (product == null)
            {
                TempData["Error"] = "Product not found or not approved.";
                return RedirectToAction("Index");
            }

            if (product.DiscountPrice > product.OriginalPrice)
            {
                TempData["Error"] = "Invalid discount policy. Discount price cannot be higher than original price.";
                return RedirectToAction("Index");
            }

            var activeVariants = await _db.PetShopProductVariants
                .Where(v => v.ProductID == productId && v.IsActive)
                .ToListAsync();

            bool hasVariants = activeVariants.Count > 0;

            PetShopProductVariant? selectedVariant = null;
            int stockLimit;

            if (hasVariants)
            {
                if (productVariantId == null)
                {
                    TempData["Error"] = "Please select a color/variant before adding to cart.";
                    return RedirectToAction("Index");
                }

                selectedVariant = activeVariants
                    .FirstOrDefault(v => v.ProductVariantID == productVariantId.Value);

                if (selectedVariant == null)
                {
                    TempData["Error"] = "Selected variant not found.";
                    return RedirectToAction("Index");
                }

                if (selectedVariant.StockQuantity <= 0)
                {
                    TempData["Error"] = "Selected variant is out of stock.";
                    return RedirectToAction("Index");
                }

                stockLimit = selectedVariant.StockQuantity;
            }
            else
            {
                if (product.StockQuantity <= 0)
                {
                    TempData["Error"] = "This product is out of stock.";
                    return RedirectToAction("Index");
                }

                stockLimit = product.StockQuantity;
            }

            var existingCartItem = await _db.PetShopCartItems
                .FirstOrDefaultAsync(c =>
                    c.UserID == userId.Value &&
                    c.ProductID == productId &&
                    c.ProductVariantID == productVariantId);

            if (existingCartItem == null)
            {
                var cartItem = new PetShopCartItem
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
                if (existingCartItem.Quantity >= stockLimit)
                {
                    TempData["Error"] = "You cannot add more than available stock.";
                    return RedirectToAction("Cart");
                }

                existingCartItem.Quantity += 1;
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = hasVariants
                ? "Selected variant added to cart."
                : "Product added to cart.";

            return RedirectToAction("Cart");
        }

        public async Task<IActionResult> Cart()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("SignIn", "Account");
            }

            var cartItems = await _db.PetShopCartItems
                .Include(c => c.Product)
                .Include(c => c.ProductVariant)
                .Where(c => c.UserID == userId.Value)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.TotalAmount = cartItems.Sum(c =>
                (c.Product != null ? c.Product.DiscountPrice : 0) * c.Quantity);

            return View(cartItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IncreaseQuantity(int cartItemId)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("SignIn", "Account");
            }

            var cartItem = await _db.PetShopCartItems
                .Include(c => c.Product)
                .Include(c => c.ProductVariant)
                .FirstOrDefaultAsync(c =>
                    c.CartItemID == cartItemId &&
                    c.UserID == userId.Value);

            if (cartItem == null || cartItem.Product == null)
            {
                return RedirectToAction("Cart");
            }

            int stockLimit = cartItem.ProductVariant != null
                ? cartItem.ProductVariant.StockQuantity
                : cartItem.Product.StockQuantity;

            if (cartItem.Quantity >= stockLimit)
            {
                TempData["Error"] = "Stock limit reached.";
                return RedirectToAction("Cart");
            }

            cartItem.Quantity += 1;
            await _db.SaveChangesAsync();

            return RedirectToAction("Cart");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DecreaseQuantity(int cartItemId)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("SignIn", "Account");
            }

            var cartItem = await _db.PetShopCartItems
                .FirstOrDefaultAsync(c =>
                    c.CartItemID == cartItemId &&
                    c.UserID == userId.Value);

            if (cartItem == null)
            {
                return RedirectToAction("Cart");
            }

            if (cartItem.Quantity <= 1)
            {
                _db.PetShopCartItems.Remove(cartItem);
            }
            else
            {
                cartItem.Quantity -= 1;
            }

            await _db.SaveChangesAsync();

            return RedirectToAction("Cart");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("SignIn", "Account");
            }

            var cartItem = await _db.PetShopCartItems
                .FirstOrDefaultAsync(c =>
                    c.CartItemID == cartItemId &&
                    c.UserID == userId.Value);

            if (cartItem != null)
            {
                _db.PetShopCartItems.Remove(cartItem);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Cart");
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("SignIn", "Account");
            }

            var cartItems = await _db.PetShopCartItems
                .Include(c => c.Product)
                .Include(c => c.ProductVariant)
                .Where(c => c.UserID == userId.Value)
                .ToListAsync();

            if (cartItems.Count == 0)
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Cart");
            }

            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserID == userId.Value);

            ViewBag.FullName = user?.FullName ?? HttpContext.Session.GetString("FullName") ?? "";
            ViewBag.Phone = user?.Phone ?? HttpContext.Session.GetString("Phone") ?? "";
            ViewBag.Address = user?.Address ?? HttpContext.Session.GetString("Address") ?? "";

            ViewBag.TotalAmount = cartItems.Sum(c =>
                (c.Product != null ? c.Product.DiscountPrice : 0) * c.Quantity);

            return View(cartItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(
            string customerName,
            string phone,
            string deliveryAddress)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("SignIn", "Account");
            }

            if (string.IsNullOrWhiteSpace(customerName) ||
                string.IsNullOrWhiteSpace(phone) ||
                string.IsNullOrWhiteSpace(deliveryAddress))
            {
                TempData["Error"] = "Please fill all checkout information.";
                return RedirectToAction("Checkout");
            }

            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserID == userId.Value);

            var cartItems = await _db.PetShopCartItems
                .Include(c => c.Product)
                .Include(c => c.ProductVariant)
                .Where(c => c.UserID == userId.Value)
                .ToListAsync();

            if (cartItems.Count == 0)
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Cart");
            }

            foreach (var item in cartItems)
            {
                if (item.Product == null ||
                    !item.Product.IsActive ||
                    item.Product.ApprovalStatus != "Approved")
                {
                    TempData["Error"] = "One or more products are not available.";
                    return RedirectToAction("Cart");
                }

                if (item.Product.DiscountPrice > item.Product.OriginalPrice)
                {
                    TempData["Error"] = $"Invalid discount policy for {item.Product.ProductName}.";
                    return RedirectToAction("Cart");
                }

                int stockLimit = item.ProductVariant != null
                    ? item.ProductVariant.StockQuantity
                    : item.Product.StockQuantity;

                if (item.ProductVariant != null && !item.ProductVariant.IsActive)
                {
                    TempData["Error"] = $"Selected variant for {item.Product.ProductName} is not active.";
                    return RedirectToAction("Cart");
                }

                if (item.Quantity > stockLimit)
                {
                    TempData["Error"] = $"Stock not enough for {item.Product.ProductName}.";
                    return RedirectToAction("Cart");
                }
            }

            decimal totalAmount = cartItems.Sum(c =>
                c.Product!.DiscountPrice * c.Quantity);

            string transactionId =
                "PHC-" +
                DateTime.Now.ToString("yyyyMMddHHmmss") +
                "-" +
                userId.Value;

            var order = new PetShopOrder
            {
                UserID = userId.Value,
                OrderNumber = transactionId,
                TransactionID = transactionId,
                TotalAmount = totalAmount,

                PaymentMethod = "SSLCommerz Sandbox",
                PaymentStatus = "Pending",
                OrderStatus = "Pending Payment",
                DeliveryStatus = "Not Shipped",

                CancellationRequested = false,
                CancellationStatus = "None",

                CustomerName = customerName.Trim(),
                Phone = phone.Trim(),
                DeliveryAddress = deliveryAddress.Trim(),

                OrderDate = DateTime.Now,
                PaymentInitiatedAt = DateTime.Now
            };

            foreach (var item in cartItems)
            {
                var orderItem = new PetShopOrderItem
                {
                    ProductID = item.ProductID,
                    ProductVariantID = item.ProductVariantID,
                    ProductName = item.Product!.ProductName,
                    VariantName = item.ProductVariant?.VariantName,
                    VariantImagePath = item.ProductVariant?.ImagePath,
                    UnitPrice = item.Product.DiscountPrice,
                    Quantity = item.Quantity,
                    LineTotal = item.Product.DiscountPrice * item.Quantity
                };

                order.OrderItems.Add(orderItem);
            }

            _db.PetShopOrders.Add(order);
            await _db.SaveChangesAsync();

            string baseUrl = $"{Request.Scheme}://{Request.Host}";

            string customerEmail =
                user?.Email ??
                HttpContext.Session.GetString("Email") ??
                "customer@pethope.com";

            try
            {
                var paymentSession =
                    await _sslCommerzService.CreatePaymentSessionAsync(
                        transactionId,
                        totalAmount,
                        customerName,
                        customerEmail,
                        phone,
                        deliveryAddress,
                        baseUrl
                    );

                if (!paymentSession.IsSuccess)
                {
                    order.PaymentStatus = "Session Failed";
                    order.OrderStatus = "Payment Session Failed";
                    order.GatewayStatus = paymentSession.Status;
                    order.GatewayPageURL = paymentSession.FailedReason;

                    await _db.SaveChangesAsync();

                    TempData["Error"] =
                        "SSLCommerz payment session failed. Please try again.";

                    return RedirectToAction("Cart");
                }

                order.SessionKey = paymentSession.SessionKey;
                order.GatewayPageURL = paymentSession.GatewayPageUrl;
                order.GatewayStatus = paymentSession.Status;

                await _db.SaveChangesAsync();

                return Redirect(paymentSession.GatewayPageUrl);
            }
            catch
            {
                order.PaymentStatus = "Session Error";
                order.OrderStatus = "Payment Session Error";

                await _db.SaveChangesAsync();

                TempData["Error"] =
                    "SSLCommerz connection error. Please check internet connection and try again.";

                return RedirectToAction("Cart");
            }
        }

        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> PaymentSuccess()
        {
            string validationId = GetRequestValue("val_id");
            string transactionId = GetRequestValue("tran_id");

            if (string.IsNullOrWhiteSpace(validationId) ||
                string.IsNullOrWhiteSpace(transactionId))
            {
                TempData["Error"] = "Invalid payment response from SSLCommerz.";
                return RedirectToAction("MyOrders");
            }

            var order = await _db.PetShopOrders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o =>
                    o.TransactionID == transactionId);

            if (order == null)
            {
                TempData["Error"] = "Payment order not found.";
                return RedirectToAction("Index");
            }

            if (order.PaymentStatus == "Paid")
            {
                return RedirectToAction("Invoice", new { id = order.OrderID });
            }

            try
            {
                var validation =
                    await _sslCommerzService.ValidatePaymentAsync(validationId);

                if (!validation.IsValid ||
                    !validation.TransactionId.Equals(transactionId, StringComparison.OrdinalIgnoreCase))
                {
                    order.PaymentStatus = "Validation Failed";
                    order.OrderStatus = "Payment Failed";
                    order.GatewayStatus = validation.Status;
                    order.ValidationID = validationId;
                    order.PaymentValidatedAt = DateTime.Now;

                    await _db.SaveChangesAsync();

                    TempData["Error"] = "Payment validation failed.";
                    return RedirectToAction("MyOrders");
                }

                bool confirmed =
                    await ConfirmOrderAfterValidPaymentAsync(order, validation);

                if (!confirmed)
                {
                    TempData["Error"] =
                        "Payment received but stock issue found. Admin will review this order.";

                    return RedirectToAction("MyOrders");
                }

                TempData["Success"] = "Payment successful. Invoice generated.";

                return RedirectToAction("Invoice", new { id = order.OrderID });
            }
            catch
            {
                order.PaymentStatus = "Validation Error";
                order.OrderStatus = "Payment Validation Error";
                order.ValidationID = validationId;
                order.PaymentValidatedAt = DateTime.Now;

                await _db.SaveChangesAsync();

                TempData["Error"] =
                    "Payment validation error. Please contact admin.";

                return RedirectToAction("MyOrders");
            }
        }

        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> PaymentFail()
        {
            string transactionId = GetRequestValue("tran_id");
            string status = GetRequestValue("status");

            if (!string.IsNullOrWhiteSpace(transactionId))
            {
                var order = await _db.PetShopOrders
                    .FirstOrDefaultAsync(o =>
                        o.TransactionID == transactionId);

                if (order != null &&
                    order.PaymentStatus != "Paid")
                {
                    order.PaymentStatus = "Failed";
                    order.OrderStatus = "Payment Failed";
                    order.DeliveryStatus = "Not Shipped";
                    order.GatewayStatus = string.IsNullOrWhiteSpace(status)
                        ? "FAILED"
                        : status;

                    await _db.SaveChangesAsync();
                }
            }

            TempData["Error"] = "Payment failed. Please try again.";
            return RedirectToAction("MyOrders");
        }

        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> PaymentCancel()
        {
            string transactionId = GetRequestValue("tran_id");
            string status = GetRequestValue("status");

            if (!string.IsNullOrWhiteSpace(transactionId))
            {
                var order = await _db.PetShopOrders
                    .FirstOrDefaultAsync(o =>
                        o.TransactionID == transactionId);

                if (order != null &&
                    order.PaymentStatus != "Paid")
                {
                    order.PaymentStatus = "Cancelled";
                    order.OrderStatus = "Payment Cancelled";
                    order.DeliveryStatus = "Not Shipped";
                    order.GatewayStatus = string.IsNullOrWhiteSpace(status)
                        ? "CANCELLED"
                        : status;

                    await _db.SaveChangesAsync();
                }
            }

            TempData["Error"] = "Payment cancelled.";
            return RedirectToAction("MyOrders");
        }

        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> PaymentIpn()
        {
            string validationId = GetRequestValue("val_id");
            string transactionId = GetRequestValue("tran_id");

            if (string.IsNullOrWhiteSpace(validationId) ||
                string.IsNullOrWhiteSpace(transactionId))
            {
                return Ok("Invalid IPN");
            }

            var order = await _db.PetShopOrders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o =>
                    o.TransactionID == transactionId);

            if (order == null)
            {
                return Ok("Order not found");
            }

            if (order.PaymentStatus == "Paid")
            {
                return Ok("Already paid");
            }

            try
            {
                var validation =
                    await _sslCommerzService.ValidatePaymentAsync(validationId);

                if (validation.IsValid &&
                    validation.TransactionId.Equals(transactionId, StringComparison.OrdinalIgnoreCase))
                {
                    await ConfirmOrderAfterValidPaymentAsync(order, validation);
                    return Ok("Payment validated");
                }

                return Ok("Payment invalid");
            }
            catch
            {
                return Ok("Validation error");
            }
        }

        public async Task<IActionResult> OrderSuccess(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("SignIn", "Account");
            }

            var order = await _db.PetShopOrders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o =>
                    o.OrderID == id &&
                    o.UserID == userId.Value);

            if (order == null)
            {
                return RedirectToAction("Index");
            }

            return View(order);
        }
        public async Task<IActionResult> Invoice(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("SignIn", "Account");
            }


            var invoice = await _db.Invoices
    .Include(i => i.User)
    .Include(i => i.Order)
        .ThenInclude(o => o.OrderItems)
    .FirstOrDefaultAsync(i =>
        i.OrderID == id &&
        i.UserID == userId.Value);

            if (invoice == null)
            {
                TempData["Error"] = "Invoice not found.";
                return RedirectToAction("MyOrders");
            }


            return View(invoice);
        }

        public async Task<IActionResult> MyOrders()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("SignIn", "Account");
            }

            var orders = await _db.PetShopOrders
                .Include(o => o.OrderItems)
                .Where(o => o.UserID == userId.Value)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestCancellation(
            int orderId,
            string cancellationReason)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("SignIn", "Account");
            }

            var order = await _db.PetShopOrders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o =>
                    o.OrderID == orderId &&
                    o.UserID == userId.Value);

            if (order == null)
            {
                return RedirectToAction("MyOrders");
            }

            if (order.PaymentStatus != "Paid")
            {
                TempData["Error"] = "Only paid and confirmed orders can request cancellation.";
                return RedirectToAction("MyOrders");
            }

            if (order.DeliveryStatus == "Delivered")
            {
                TempData["Error"] = "Delivered order cannot be cancelled.";
                return RedirectToAction("MyOrders");
            }

            if (order.OrderStatus == "Cancelled")
            {
                TempData["Error"] = "This order is already cancelled.";
                return RedirectToAction("MyOrders");
            }

            if (string.IsNullOrWhiteSpace(cancellationReason))
            {
                cancellationReason = "Customer requested cancellation.";
            }

            if (order.OrderStatus == "Pending")
            {
                order.OrderStatus = "Cancelled";
                order.CancellationRequested = true;
                order.CancellationStatus = "Approved";
                order.CancellationReason = cancellationReason;
                order.CancelledAt = DateTime.Now;

                foreach (var item in order.OrderItems)
                {
                    var product = await _db.PetShopProducts
                        .FirstOrDefaultAsync(p => p.ProductID == item.ProductID);

                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity;
                    }

                    if (item.ProductVariantID != null)
                    {
                        var variant = await _db.PetShopProductVariants
                            .FirstOrDefaultAsync(v =>
                                v.ProductVariantID == item.ProductVariantID.Value);

                        if (variant != null)
                        {
                            variant.StockQuantity += item.Quantity;
                            await SyncProductStockFromVariantsAsync(variant.ProductID);
                        }
                    }
                }
            }
            else
            {
                order.CancellationRequested = true;
                order.CancellationStatus = "Pending Approval";
                order.CancellationReason = cancellationReason;
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = "Cancellation request submitted.";
            return RedirectToAction("MyOrders");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitComplaint(
            int orderId,
            string complaintType,
            string complaintMessage)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("SignIn", "Account");
            }

            var orderExists = await _db.PetShopOrders
                .AnyAsync(o =>
                    o.OrderID == orderId &&
                    o.UserID == userId.Value &&
                    o.PaymentStatus == "Paid");

            if (!orderExists)
            {
                TempData["Error"] = "Only paid orders can submit complaint.";
                return RedirectToAction("MyOrders");
            }

            if (string.IsNullOrWhiteSpace(complaintType) ||
                string.IsNullOrWhiteSpace(complaintMessage))
            {
                TempData["Error"] = "Complaint type and message are required.";
                return RedirectToAction("MyOrders");
            }

            var complaint = new PetShopComplaint
            {
                UserID = userId.Value,
                OrderID = orderId,
                ComplaintType = complaintType.Trim(),
                ComplaintMessage = complaintMessage.Trim(),
                ComplaintStatus = "Pending",
                CreatedAt = DateTime.Now
            };

            _db.PetShopComplaints.Add(complaint);
            await _db.SaveChangesAsync();

            await CreateShopComplaintNotificationAsync(complaint);

            TempData["Success"] = "Complaint submitted successfully.";
            return RedirectToAction("MyOrders");
        }

        private async Task<bool> ConfirmOrderAfterValidPaymentAsync(
            PetShopOrder order,
            SslCommerzValidationResult validation)
        {
            if (order.PaymentStatus == "Paid")
            {
                return true;
            }

            foreach (var item in order.OrderItems)
            {
                var product = await _db.PetShopProducts
                    .FirstOrDefaultAsync(p => p.ProductID == item.ProductID);

                if (product == null ||
                    !product.IsActive ||
                    product.ApprovalStatus != "Approved" ||
                    product.DiscountPrice > product.OriginalPrice)
                {
                    await MarkOrderAsStockIssueAsync(order, validation);
                    return false;
                }

                if (item.ProductVariantID != null)
                {
                    var variant = await _db.PetShopProductVariants
                        .FirstOrDefaultAsync(v =>
                            v.ProductVariantID == item.ProductVariantID.Value);

                    if (variant == null ||
                        !variant.IsActive ||
                        variant.StockQuantity < item.Quantity)
                    {
                        await MarkOrderAsStockIssueAsync(order, validation);
                        return false;
                    }
                }
                else
                {
                    if (product.StockQuantity < item.Quantity)
                    {
                        await MarkOrderAsStockIssueAsync(order, validation);
                        return false;
                    }
                }
            }

            foreach (var item in order.OrderItems)
            {
                if (item.ProductVariantID != null)
                {
                    var variant = await _db.PetShopProductVariants
                        .FirstOrDefaultAsync(v =>
                            v.ProductVariantID == item.ProductVariantID.Value);

                    if (variant != null)
                    {
                        variant.StockQuantity -= item.Quantity;
                        await SyncProductStockFromVariantsAsync(variant.ProductID);
                    }
                }
                else
                {
                    var product = await _db.PetShopProducts
                        .FirstOrDefaultAsync(p => p.ProductID == item.ProductID);

                    if (product != null)
                    {
                        product.StockQuantity -= item.Quantity;
                    }
                }
            }

            order.PaymentMethod = string.IsNullOrWhiteSpace(validation.CardType)
                ? "SSLCommerz"
                : validation.CardType;

            order.PaymentStatus = "Paid";
            order.OrderStatus = "Confirmed";
            order.DeliveryStatus = "Processing";

            order.GatewayStatus = validation.Status;
            order.ValidationID = validation.ValidationId;
            order.BankTransactionID = validation.BankTransactionId;
            order.CardType = validation.CardType;
            order.CardIssuer = validation.CardIssuer;
            order.CardBrand = validation.CardBrand;
            order.StoreAmount = validation.StoreAmount;
            order.RiskLevel = validation.RiskLevel;
            order.RiskTitle = validation.RiskTitle;

            order.ConfirmedAt = DateTime.Now;
            order.PaymentValidatedAt = DateTime.Now;
            // Save Payment Transaction History

            var paymentTransaction = new PaymentTransaction
            {
                UserID = order.UserID,

                OrderID = order.OrderID,

                Amount = order.TotalAmount,

                PaymentMethod = string.IsNullOrEmpty(validation.CardType)
                    ? "SSLCommerz"
                    : validation.CardType,

                TransactionStatus = "Paid",

                GatewayTransactionID = validation.BankTransactionId,

                TransactionDate = DateTime.Now
            };

            _db.PaymentTransactions.Add(paymentTransaction);
            await _db.SaveChangesAsync();
            // =========================
            // CREATE INVOICE AFTER SUCCESSFUL PAYMENT
            // =========================

            var existingInvoice = await _db.Invoices
                .FirstOrDefaultAsync(i => i.OrderID == order.OrderID);

            if (existingInvoice == null)
            {
                var invoice = new Invoice
                {
                    OrderID = order.OrderID,
                    UserID = order.UserID,

                    InvoiceNumber =
                        "INV-" + DateTime.Now.ToString("yyyyMMddHHmmss"),

                    Amount = order.TotalAmount,

                    PaymentMethod = string.IsNullOrWhiteSpace(validation.CardType)
                        ? "SSLCommerz"
                        : validation.CardType,

                    PaymentStatus = "Paid",

                    InvoiceDate = DateTime.Now
                };

                _db.Invoices.Add(invoice);

                await _db.SaveChangesAsync();
            }

            var cartItems = await _db.PetShopCartItems
                .Where(c => c.UserID == order.UserID)
                .ToListAsync();

            if (cartItems.Count > 0)
            {
                _db.PetShopCartItems.RemoveRange(cartItems);
            }

            await _db.SaveChangesAsync();

            await CreateShopOrderNotificationAsync(order);

            return true;
        }

        private async Task MarkOrderAsStockIssueAsync(
            PetShopOrder order,
            SslCommerzValidationResult validation)
        {
            order.PaymentStatus = "Paid - Stock Issue";
            order.OrderStatus = "Manual Review";
            order.DeliveryStatus = "Hold";
            order.GatewayStatus = validation.Status;
            order.ValidationID = validation.ValidationId;
            order.BankTransactionID = validation.BankTransactionId;
            order.CardType = validation.CardType;
            order.CardIssuer = validation.CardIssuer;
            order.CardBrand = validation.CardBrand;
            order.StoreAmount = validation.StoreAmount;
            order.RiskLevel = validation.RiskLevel;
            order.RiskTitle = validation.RiskTitle;
            order.PaymentValidatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            await CreateShopOrderStockIssueNotificationAsync(order);
        }

        private async Task CreateShopOrderNotificationAsync(PetShopOrder order)
        {
            bool alreadyCreated = await _db.Notifications.AnyAsync(n =>
                n.Title == "New Shop Order" &&
                n.Link == "/Admin/PetShop/Orders" &&
                n.Message.Contains("Order ID: #" + order.OrderID));

            if (alreadyCreated)
            {
                return;
            }

            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserID == order.UserID);

            string adopterName = user?.FullName ?? order.CustomerName ?? "An adopter";

            var notification = new Notification
            {
                Title = "New Shop Order",
                Message = adopterName
                          + " placed a new Pet Care Shop order. Order ID: #"
                          + order.OrderID
                          + ", Amount: ৳"
                          + order.TotalAmount.ToString("0.00"),
                Link = "/Admin/PetShop/Orders",
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();
        }

        private async Task CreateShopOrderStockIssueNotificationAsync(PetShopOrder order)
        {
            var notification = new Notification
            {
                Title = "Shop Order Stock Issue",
                Message = "Payment received but stock issue found for Order ID: #"
                          + order.OrderID
                          + ", Amount: ৳"
                          + order.TotalAmount.ToString("0.00"),
                Link = "/Admin/PetShop/Orders",
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();
        }

        private async Task CreateShopComplaintNotificationAsync(PetShopComplaint complaint)
        {
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserID == complaint.UserID);

            string adopterName = user?.FullName ?? "An adopter";

            var notification = new Notification
            {
                Title = "New Shop Complaint",
                Message = adopterName
                          + " submitted a complaint for Order ID: #"
                          + complaint.OrderID
                          + ". Type: "
                          + complaint.ComplaintType,
                Link = "/Admin/PetShop/Complaints",
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();
        }

        private async Task SyncProductStockFromVariantsAsync(int productId)
        {
            bool hasVariants = await _db.PetShopProductVariants
                .AnyAsync(v => v.ProductID == productId);

            if (!hasVariants)
            {
                return;
            }

            var product = await _db.PetShopProducts
                .FirstOrDefaultAsync(p => p.ProductID == productId);

            if (product == null)
            {
                return;
            }

            var activeVariants = await _db.PetShopProductVariants
                .Where(v => v.ProductID == productId && v.IsActive)
                .ToListAsync();

            product.StockQuantity = activeVariants.Sum(v => v.StockQuantity);

            string? primaryImage = activeVariants
                .FirstOrDefault(v =>
                    v.StockQuantity > 0 &&
                    !string.IsNullOrWhiteSpace(v.ImagePath))
                ?.ImagePath;

            if (string.IsNullOrWhiteSpace(primaryImage))
            {
                primaryImage = activeVariants
                    .FirstOrDefault(v =>
                        !string.IsNullOrWhiteSpace(v.ImagePath))
                    ?.ImagePath;
            }

            if (!string.IsNullOrWhiteSpace(primaryImage))
            {
                product.ImagePath = primaryImage;
            }
        }

        private string GetRequestValue(string key)
        {
            if (Request.HasFormContentType &&
                Request.Form.ContainsKey(key))
            {
                return Request.Form[key].ToString();
            }

            if (Request.Query.ContainsKey(key))
            {
                return Request.Query[key].ToString();
            }

            return string.Empty;
        }

        private async Task<int> GetCartCountAsync()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return 0;
            }

            return await _db.PetShopCartItems
                .Where(c => c.UserID == userId.Value)
                .SumAsync(c => c.Quantity);
        }

        private async Task SeedProductsIfEmptyAsync()
        {
            bool hasProducts = await _db.PetShopProducts.AnyAsync();

            if (hasProducts)
            {
                return;
            }

            var products = new List<PetShopProduct>
            {
                new PetShopProduct
                {
                    ProductName = "Premium Cat Food",
                    ProductCategory = "Food",
                    PetType = "Cat",
                    OriginalPrice = 650,
                    DiscountPrice = 520,
                    StockQuantity = 20,
                    LowStockLimit = 5,
                    ApprovalStatus = "Approved",
                    IsActive = true,
                    Description = "Discounted healthy food for cats."
                },
                new PetShopProduct
                {
                    ProductName = "Healthy Dog Food",
                    ProductCategory = "Food",
                    PetType = "Dog",
                    OriginalPrice = 750,
                    DiscountPrice = 620,
                    StockQuantity = 18,
                    LowStockLimit = 5,
                    ApprovalStatus = "Approved",
                    IsActive = true,
                    Description = "Affordable dog food for adopted pets."
                },
                new PetShopProduct
                {
                    ProductName = "Cat Collar",
                    ProductCategory = "Accessory",
                    PetType = "Cat",
                    OriginalPrice = 180,
                    DiscountPrice = 130,
                    StockQuantity = 30,
                    LowStockLimit = 5,
                    ApprovalStatus = "Approved",
                    IsActive = true,
                    Description = "Comfortable collar for cats."
                },
                new PetShopProduct
                {
                    ProductName = "Dog Belt",
                    ProductCategory = "Accessory",
                    PetType = "Dog",
                    OriginalPrice = 250,
                    DiscountPrice = 190,
                    StockQuantity = 25,
                    LowStockLimit = 5,
                    ApprovalStatus = "Approved",
                    IsActive = true,
                    Description = "Strong and safe belt for dogs."
                },
                new PetShopProduct
                {
                    ProductName = "Cat Litter",
                    ProductCategory = "Care",
                    PetType = "Cat",
                    OriginalPrice = 450,
                    DiscountPrice = 360,
                    StockQuantity = 15,
                    LowStockLimit = 4,
                    ApprovalStatus = "Approved",
                    IsActive = true,
                    Description = "Low-price litter support for cat adopters."
                },
                new PetShopProduct
                {
                    ProductName = "Bird Cage",
                    ProductCategory = "Accessory",
                    PetType = "Bird",
                    OriginalPrice = 1200,
                    DiscountPrice = 950,
                    StockQuantity = 8,
                    LowStockLimit = 2,
                    ApprovalStatus = "Approved",
                    IsActive = true,
                    Description = "Affordable cage for adopted birds."
                },
                new PetShopProduct
                {
                    ProductName = "Bird Food",
                    ProductCategory = "Food",
                    PetType = "Bird",
                    OriginalPrice = 300,
                    DiscountPrice = 240,
                    StockQuantity = 20,
                    LowStockLimit = 5,
                    ApprovalStatus = "Approved",
                    IsActive = true,
                    Description = "Discounted bird food."
                },
                new PetShopProduct
                {
                    ProductName = "Pet Feeding Bowl",
                    ProductCategory = "Accessory",
                    PetType = "All",
                    OriginalPrice = 220,
                    DiscountPrice = 160,
                    StockQuantity = 22,
                    LowStockLimit = 5,
                    ApprovalStatus = "Approved",
                    IsActive = true,
                    Description = "Feeding bowl for cats, dogs and birds."
                },
                new PetShopProduct
                {
                    ProductName = "Pet Shampoo",
                    ProductCategory = "Care",
                    PetType = "All",
                    OriginalPrice = 350,
                    DiscountPrice = 270,
                    StockQuantity = 14,
                    LowStockLimit = 4,
                    ApprovalStatus = "Approved",
                    IsActive = true,
                    Description = "Basic grooming care item."
                },
                new PetShopProduct
                {
                    ProductName = "Vet Checkup Coupon",
                    ProductCategory = "Vet Care",
                    PetType = "All",
                    OriginalPrice = 500,
                    DiscountPrice = 350,
                    StockQuantity = 10,
                    LowStockLimit = 3,
                    ApprovalStatus = "Approved",
                    IsActive = true,
                    Description = "Discount coupon for basic veterinary checkup."
                }
            };

            _db.PetShopProducts.AddRange(products);
            await _db.SaveChangesAsync();
        }
    }
}