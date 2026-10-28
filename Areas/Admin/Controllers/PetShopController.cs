using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Models;
using PetWAMS.Web.Services;
using PetWAMS.Web.ViewModels;

namespace PetWAMS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PetShopController : Controller
    {
        private readonly PetWamsDbContext _db;
        private readonly SslCommerzService _sslCommerzService;

        public PetShopController(
            PetWamsDbContext db,
            SslCommerzService sslCommerzService)
        {
            _db = db;
            _sslCommerzService = sslCommerzService;
        }

        // =========================
        // ADMIN LOGIN CHECK
        // =========================
        private bool IsAdminLoggedIn()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            string? userType = HttpContext.Session.GetString("UserType");

            return userId != null &&
                   !string.IsNullOrWhiteSpace(userType) &&
                   userType.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }

        // =========================
        // PRODUCT MANAGEMENT
        // =========================
        public async Task<IActionResult> Index()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }

            await SeedProductsIfEmptyAsync();

            var products = await _db.PetShopProducts
                .AsNoTracking()
                .OrderBy(p => p.ApprovalStatus == "Pending" ? 0 : 1)
                .ThenBy(p => p.ProductCategory)
                .ThenBy(p => p.ProductName)
                .ToListAsync();

            ViewBag.TotalProducts = products.Count;
            ViewBag.PendingProducts = products.Count(p => p.ApprovalStatus == "Pending");
            ViewBag.ApprovedProducts = products.Count(p => p.ApprovalStatus == "Approved");
            ViewBag.LowStockProducts = products.Count(p => p.StockQuantity <= p.LowStockLimit);

            return View(products);
        }

        // =========================
        // ADD PRODUCT
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(
            string productName,
            string productCategory,
            string petType,
            decimal originalPrice,
            decimal discountPrice,
            int stockQuantity,
            int lowStockLimit,
            string? description)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }

            if (string.IsNullOrWhiteSpace(productName) ||
                string.IsNullOrWhiteSpace(productCategory) ||
                string.IsNullOrWhiteSpace(petType))
            {
                TempData["Error"] = "Product name, category and pet type are required.";
                return RedirectToAction("Index");
            }

            if (originalPrice <= 0 || discountPrice <= 0)
            {
                TempData["Error"] = "Price must be greater than zero.";
                return RedirectToAction("Index");
            }

            if (discountPrice > originalPrice)
            {
                TempData["Error"] = "Discount price cannot be higher than original price.";
                return RedirectToAction("Index");
            }

            if (stockQuantity < 0)
            {
                TempData["Error"] = "Stock cannot be negative.";
                return RedirectToAction("Index");
            }

            if (lowStockLimit < 0)
            {
                lowStockLimit = 0;
            }

            var product = new PetShopProduct
            {
                ProductName = productName.Trim(),
                ProductCategory = productCategory.Trim(),
                PetType = petType.Trim(),
                OriginalPrice = originalPrice,
                DiscountPrice = discountPrice,
                StockQuantity = stockQuantity,
                LowStockLimit = lowStockLimit,
                ApprovalStatus = "Pending",
                IsActive = false,
                Description = description,
                CreatedAt = DateTime.Now
            };

            _db.PetShopProducts.Add(product);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Product added as Pending. Admin must approve it before showing in shop.";
            return RedirectToAction("Index");
        }

        // =========================
        // UPDATE PRODUCT
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProduct(
            int productId,
            decimal originalPrice,
            decimal discountPrice,
            int stockQuantity,
            int lowStockLimit)
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

            if (originalPrice <= 0 || discountPrice <= 0)
            {
                TempData["Error"] = "Price must be greater than zero.";
                return RedirectToAction("Index");
            }

            if (discountPrice > originalPrice)
            {
                TempData["Error"] = "Discount price cannot be higher than original price.";
                return RedirectToAction("Index");
            }

            if (stockQuantity < 0)
            {
                TempData["Error"] = "Stock cannot be negative.";
                return RedirectToAction("Index");
            }

            if (lowStockLimit < 0)
            {
                lowStockLimit = 0;
            }

            product.OriginalPrice = originalPrice;
            product.DiscountPrice = discountPrice;
            product.StockQuantity = stockQuantity;
            product.LowStockLimit = lowStockLimit;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Product price and stock updated.";
            return RedirectToAction("Index");
        }

        // =========================
        // APPROVE PRODUCT
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProduct(int productId)
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

            if (product.DiscountPrice > product.OriginalPrice)
            {
                TempData["Error"] = "Cannot approve. Discount price is higher than original price.";
                return RedirectToAction("Index");
            }

            if (product.StockQuantity <= 0)
            {
                TempData["Error"] = "Cannot approve. Product stock must be greater than zero.";
                return RedirectToAction("Index");
            }

            product.ApprovalStatus = "Approved";
            product.IsActive = true;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Product approved and visible in Pet Care Shop.";
            return RedirectToAction("Index");
        }

        // =========================
        // REJECT PRODUCT
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectProduct(int productId)
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

            product.ApprovalStatus = "Rejected";
            product.IsActive = false;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Product rejected.";
            return RedirectToAction("Index");
        }

        // =========================
        // ACTIVATE / DEACTIVATE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleProductStatus(int productId)
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

            if (product.ApprovalStatus != "Approved")
            {
                TempData["Error"] = "Only approved products can be activated or deactivated.";
                return RedirectToAction("Index");
            }

            product.IsActive = !product.IsActive;

            await _db.SaveChangesAsync();

            TempData["Success"] = product.IsActive
                ? "Product activated."
                : "Product deactivated.";

            return RedirectToAction("Index");
        }
        // =========================
        // VIEW INVOICE (ADMIN)
        // =========================
        public async Task<IActionResult> Invoice(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }


            var invoice = await _db.Invoices
                .Include(i => i.User)
                .Include(i => i.Order)
                    .ThenInclude(o => o.OrderItems)
                .FirstOrDefaultAsync(i => i.OrderID == id);


            if (invoice == null)
            {
                TempData["Error"] = "Invoice not found.";
                return RedirectToAction("Orders");
            }


            return View(invoice);
        }

        // =========================
        // ORDER MANAGEMENT
        // =========================
        public async Task<IActionResult> Orders()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }

            var orders = await _db.PetShopOrders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.TotalOrders = orders.Count;
            ViewBag.ConfirmedOrders = orders.Count(o => o.OrderStatus == "Confirmed");
            ViewBag.DeliveredOrders = orders.Count(o => o.DeliveryStatus == "Delivered");
            ViewBag.CancellationRequests = orders.Count(o => o.CancellationRequested && o.CancellationStatus == "Pending Approval");

            return View(orders);
        }

        // =========================
        // UPDATE DELIVERY STATUS
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDeliveryStatus(
            int orderId,
            string deliveryStatus)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }

            var order = await _db.PetShopOrders
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Orders");
            }

            if (order.OrderStatus == "Cancelled")
            {
                TempData["Error"] = "Cancelled order delivery status cannot be updated.";
                return RedirectToAction("Orders");
            }

            if (string.IsNullOrWhiteSpace(deliveryStatus))
            {
                TempData["Error"] = "Delivery status is required.";
                return RedirectToAction("Orders");
            }

            order.DeliveryStatus = deliveryStatus.Trim();

            if (order.DeliveryStatus == "Delivered")
            {
                order.DeliveredAt = DateTime.Now;
                order.OrderStatus = "Completed";
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = "Delivery status updated.";
            return RedirectToAction("Orders");
        }

        // =========================
        // APPROVE CANCELLATION
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveCancellation(int orderId)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }

            var order = await _db.PetShopOrders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Orders");
            }

            if (!order.CancellationRequested)
            {
                TempData["Error"] = "No cancellation request found for this order.";
                return RedirectToAction("Orders");
            }

            if (order.DeliveryStatus == "Delivered")
            {
                order.CancellationStatus = "Rejected";
                TempData["Error"] = "Delivered order cannot be cancelled.";
                await _db.SaveChangesAsync();
                return RedirectToAction("Orders");
            }

            if (order.OrderStatus == "Cancelled")
            {
                TempData["Error"] = "Order already cancelled.";
                return RedirectToAction("Orders");
            }

            order.OrderStatus = "Cancelled";
            order.DeliveryStatus = "Cancelled";
            order.CancellationStatus = "Approved";
            order.CancelledAt = DateTime.Now;
            order.PaymentStatus = "Refund Pending";

            foreach (var item in order.OrderItems)
            {
                var product = await _db.PetShopProducts
                    .FirstOrDefaultAsync(p => p.ProductID == item.ProductID);

                if (product != null)
                {
                    product.StockQuantity += item.Quantity;
                }
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = "Cancellation approved. Stock restored and refund marked as pending.";
            return RedirectToAction("Orders");
        }

        // =========================
        // REJECT CANCELLATION
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectCancellation(int orderId)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }

            var order = await _db.PetShopOrders
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Orders");
            }

            if (!order.CancellationRequested)
            {
                TempData["Error"] = "No cancellation request found for this order.";
                return RedirectToAction("Orders");
            }

            order.CancellationStatus = "Rejected";

            await _db.SaveChangesAsync();

            TempData["Success"] = "Cancellation request rejected.";
            return RedirectToAction("Orders");
        }
        // =========================
        // INITIATE REAL SSLCOMMERZ REFUND
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteRefund(int orderId)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }

            var order = await _db.PetShopOrders
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Orders");
            }

            if (order.PaymentStatus != "Refund Pending")
            {
                TempData["Error"] = "This order is not waiting for refund.";
                return RedirectToAction("Orders");
            }

            if (order.CancellationStatus != "Approved" ||
                order.OrderStatus != "Cancelled")
            {
                TempData["Error"] = "Refund can only be initiated for an approved cancelled order.";
                return RedirectToAction("Orders");
            }

            if (string.IsNullOrWhiteSpace(order.BankTransactionID))
            {
                order.RefundStatus = "Failed";
                order.RefundGatewayStatus = "failed";
                order.RefundGatewayMessage =
                    "Bank transaction ID is missing. Automatic SSLCommerz refund cannot be initiated.";
                order.RefundNote =
                    "Automatic gateway refund was not initiated because the original SSLCommerz bank transaction ID is missing.";

                await _db.SaveChangesAsync();

                TempData["Error"] =
                    "Bank transaction ID is missing. This payment cannot be automatically refunded through SSLCommerz.";

                return RedirectToAction("Orders");
            }

            if (order.TotalAmount <= 0)
            {
                TempData["Error"] = "Refund amount must be greater than zero.";
                return RedirectToAction("Orders");
            }

            // Safety: never send the same order to the refund API twice
            // after SSLCommerz has already returned a refund reference ID.
            if (!string.IsNullOrWhiteSpace(order.RefundReferenceID))
            {
                TempData["Error"] =
                    "A refund has already been initiated for this order. Use Check Refund Status instead of creating another refund.";

                return RedirectToAction("Orders");
            }

            // SSLCommerz requires a unique refund_trans_id (maximum 30 characters).
            string refundTransactionId =
                $"RF{order.OrderID}{DateTime.UtcNow:yyMMddHHmmssfff}";

            if (refundTransactionId.Length > 30)
            {
                refundTransactionId = refundTransactionId.Substring(0, 30);
            }

            string refundRemarks =
                $"Refund for cancelled PetHope order {order.OrderNumber}";

            string referenceId = string.IsNullOrWhiteSpace(order.OrderNumber)
                ? $"ORDER-{order.OrderID}"
                : order.OrderNumber;

            // Store the ID before the API request so every attempt is traceable.
            order.RefundTransactionID = refundTransactionId;
            order.RefundAmount = order.TotalAmount;
            order.RefundInitiatedAt = DateTime.Now;
            order.RefundGatewayStatus = "requesting";
            order.RefundGatewayMessage = "Refund request is being sent to SSLCommerz.";
            order.RefundStatus = "Processing";
            order.RefundNote = "SSLCommerz refund initiation requested by admin.";

            await _db.SaveChangesAsync();

            var refundResult = await _sslCommerzService.InitiateRefundAsync(
                order.BankTransactionID,
                refundTransactionId,
                order.TotalAmount,
                refundRemarks,
                referenceId);

            order.RefundGatewayStatus =
                string.IsNullOrWhiteSpace(refundResult.Status)
                    ? "failed"
                    : refundResult.Status;

            order.RefundGatewayMessage =
                string.IsNullOrWhiteSpace(refundResult.ErrorReason)
                    ? $"SSLCommerz API connection status: {refundResult.ApiConnect}"
                    : refundResult.ErrorReason;

            if (refundResult.IsSuccess)
            {
                order.RefundReferenceID = refundResult.RefundReferenceId;
                order.PaymentStatus = "Refund Processing";
                order.RefundStatus = "Processing";
                order.RefundNote =
                    "Refund successfully initiated through SSLCommerz. Waiting for final gateway confirmation.";

                await _db.SaveChangesAsync();

                TempData["Success"] =
                    "SSLCommerz refund initiated successfully. Now use Check Refund Status until the gateway confirms it as refunded.";

                return RedirectToAction("Orders");
            }

            // If the gateway rejected the request, keep the order eligible
            // for a later retry. Do not mark money as refunded.
            order.PaymentStatus = "Refund Pending";
            order.RefundStatus = "Failed";
            order.RefundNote =
                "SSLCommerz refund initiation failed. No refund has been marked as completed.";

            await _db.SaveChangesAsync();

            TempData["Error"] =
                "SSLCommerz refund could not be initiated: " +
                (string.IsNullOrWhiteSpace(refundResult.ErrorReason)
                    ? "Unknown gateway error."
                    : refundResult.ErrorReason);

            return RedirectToAction("Orders");
        }

        // =========================
        // CHECK REAL REFUND STATUS
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckRefundStatus(int orderId)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }

            var order = await _db.PetShopOrders
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Orders");
            }

            if (string.IsNullOrWhiteSpace(order.RefundReferenceID))
            {
                TempData["Error"] =
                    "No SSLCommerz refund reference ID was found for this order. Initiate the refund first.";

                return RedirectToAction("Orders");
            }

            var statusResult = await _sslCommerzService.QueryRefundStatusAsync(
                order.RefundReferenceID);

            order.RefundLastCheckedAt = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(statusResult.Status))
            {
                order.RefundGatewayStatus = statusResult.Status;
            }

            order.RefundGatewayMessage =
                string.IsNullOrWhiteSpace(statusResult.ErrorReason)
                    ? $"SSLCommerz API connection status: {statusResult.ApiConnect}"
                    : statusResult.ErrorReason;

            if (!statusResult.IsSuccess)
            {
                await _db.SaveChangesAsync();

                TempData["Error"] =
                    "Could not confirm refund status from SSLCommerz: " +
                    (string.IsNullOrWhiteSpace(statusResult.ErrorReason)
                        ? "Gateway status query failed."
                        : statusResult.ErrorReason);

                return RedirectToAction("Orders");
            }

            if (statusResult.IsRefunded)
            {
                order.PaymentStatus = "Refunded";
                order.RefundStatus = "Completed";
                order.RefundAmount = order.TotalAmount;
                order.RefundCompletedAt = statusResult.RefundedOn ?? DateTime.Now;
                order.RefundNote =
                    "Refund confirmed as completed by SSLCommerz.";

                await _db.SaveChangesAsync();

                TempData["Success"] =
                    "SSLCommerz confirmed that the refund has been completed.";

                return RedirectToAction("Orders");
            }

            if (statusResult.IsProcessing)
            {
                order.PaymentStatus = "Refund Processing";
                order.RefundStatus = "Processing";
                order.RefundNote =
                    "SSLCommerz is still processing this refund.";

                await _db.SaveChangesAsync();

                TempData["Success"] =
                    "Refund is still processing at SSLCommerz. Check again later.";

                return RedirectToAction("Orders");
            }

            if (statusResult.IsCancelled)
            {
                order.PaymentStatus = "Refund Cancelled";
                order.RefundStatus = "Cancelled";
                order.RefundNote =
                    "SSLCommerz reports that this refund was cancelled.";

                await _db.SaveChangesAsync();

                TempData["Error"] =
                    "SSLCommerz reports that the refund was cancelled. Review it in the merchant panel before trying any further action.";

                return RedirectToAction("Orders");
            }

            order.RefundStatus =
                string.IsNullOrWhiteSpace(statusResult.Status)
                    ? order.RefundStatus
                    : statusResult.Status;

            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"Latest SSLCommerz refund status: {statusResult.Status}.";

            return RedirectToAction("Orders");
        }

        // =========================
        // COMPLAINT MANAGEMENT
        // =========================
        public async Task<IActionResult> Complaints()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }

            var complaints = await _db.PetShopComplaints
                .Include(c => c.User)
                .Include(c => c.Order)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.TotalComplaints = complaints.Count;
            ViewBag.PendingComplaints = complaints.Count(c => c.ComplaintStatus == "Pending");
            ViewBag.ResolvedComplaints = complaints.Count(c => c.ComplaintStatus == "Resolved");

            return View(complaints);
        }

        // =========================
        // RESOLVE COMPLAINT
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveComplaint(
            int complaintId,
            string adminReply)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }

            var complaint = await _db.PetShopComplaints
                .FirstOrDefaultAsync(c => c.ComplaintID == complaintId);

            if (complaint == null)
            {
                TempData["Error"] = "Complaint not found.";
                return RedirectToAction("Complaints");
            }

            if (string.IsNullOrWhiteSpace(adminReply))
            {
                TempData["Error"] = "Admin reply is required.";
                return RedirectToAction("Complaints");
            }

            complaint.AdminReply = adminReply.Trim();
            complaint.ComplaintStatus = "Resolved";
            complaint.ResolvedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Complaint resolved successfully.";
            return RedirectToAction("Complaints");
        }

        // =========================
        // SHOP REPORT
        // =========================
        public async Task<IActionResult> Report()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }

            List<PetShopProduct> products = await _db.PetShopProducts
                .AsNoTracking()
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            List<PetShopOrder> orders = await _db.PetShopOrders
                .Include(o => o.OrderItems)
                .AsNoTracking()
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            List<PetShopComplaint> complaints = await _db.PetShopComplaints
                .AsNoTracking()
                .ToListAsync();

            DateTime today = DateTime.Today;

            int daysSinceMonday =
                ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;

            DateTime weekStart =
                today.AddDays(-daysSinceMonday);

            DateTime nextWeekStart =
                weekStart.AddDays(7);

            DateTime monthStart =
                new DateTime(today.Year, today.Month, 1);

            DateTime nextMonthStart =
                monthStart.AddMonths(1);

            List<PetShopOrder> validSalesOrders = orders
                .Where(IsValidSalesOrder)
                .ToList();

            List<PetShopOrder> weeklyOrders = validSalesOrders
                .Where(o =>
                    o.OrderDate >= weekStart &&
                    o.OrderDate < nextWeekStart)
                .ToList();

            List<PetShopOrder> monthlyOrders = validSalesOrders
                .Where(o =>
                    o.OrderDate >= monthStart &&
                    o.OrderDate < nextMonthStart)
                .ToList();

            var model = new PetShopSalesReportViewModel
            {
                GeneratedAt = DateTime.Now,

                WeekStart = weekStart,
                WeekEnd = nextWeekStart.AddDays(-1),
                WeeklyOrderCount = weeklyOrders.Count,
                WeeklyUnitsSold = weeklyOrders
                    .SelectMany(o => o.OrderItems)
                    .Sum(i => i.Quantity),
                WeeklyRevenue = weeklyOrders
                    .SelectMany(o => o.OrderItems)
                    .Sum(i => i.LineTotal),
                WeeklyProductSales = BuildProductSalesRanking(
                    products,
                    weeklyOrders),

                MonthStart = monthStart,
                MonthEnd = nextMonthStart.AddDays(-1),
                MonthlyOrderCount = monthlyOrders.Count,
                MonthlyUnitsSold = monthlyOrders
                    .SelectMany(o => o.OrderItems)
                    .Sum(i => i.Quantity),
                MonthlyRevenue = monthlyOrders
                    .SelectMany(o => o.OrderItems)
                    .Sum(i => i.LineTotal),
                MonthlyProductSales = BuildProductSalesRanking(
                    products,
                    monthlyOrders)
            };

            ViewBag.TotalProducts = products.Count;
            ViewBag.PendingProducts = products.Count(p => p.ApprovalStatus == "Pending");
            ViewBag.ApprovedProducts = products.Count(p => p.ApprovalStatus == "Approved");
            ViewBag.RejectedProducts = products.Count(p => p.ApprovalStatus == "Rejected");
            ViewBag.LowStockProducts = products.Count(p => p.StockQuantity <= p.LowStockLimit);

            ViewBag.TotalOrders = orders.Count;
            ViewBag.ConfirmedOrders = orders.Count(o => o.OrderStatus == "Confirmed");
            ViewBag.CompletedOrders = orders.Count(o => o.OrderStatus == "Completed");
            ViewBag.CancelledOrders = orders.Count(o => o.OrderStatus == "Cancelled");
            ViewBag.DeliveredOrders = orders.Count(o => o.DeliveryStatus == "Delivered");
            ViewBag.CancellationRequests = orders.Count(o =>
                o.CancellationRequested &&
                o.CancellationStatus == "Pending Approval");

            ViewBag.TotalRevenue = validSalesOrders
                .Sum(o => o.TotalAmount);

            ViewBag.TotalComplaints = complaints.Count;
            ViewBag.PendingComplaints = complaints.Count(c => c.ComplaintStatus == "Pending");
            ViewBag.ResolvedComplaints = complaints.Count(c => c.ComplaintStatus == "Resolved");

            ViewBag.LowStockList = products
                .Where(p => p.StockQuantity <= p.LowStockLimit)
                .OrderBy(p => p.StockQuantity)
                .ToList();

            ViewBag.RecentOrders = orders
                .Take(5)
                .ToList();

            return View(model);
        }

        // Counts only successful sales in sales rankings and revenue totals.
        private static bool IsValidSalesOrder(
            PetShopOrder order)
        {
            if (order == null)
            {
                return false;
            }

            bool isPaid = string.Equals(
                order.PaymentStatus,
                "Paid",
                StringComparison.OrdinalIgnoreCase);

            bool isCancelled = string.Equals(
                order.OrderStatus,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase);

            bool isRefunded =
                string.Equals(
                    order.PaymentStatus,
                    "Refunded",
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    order.RefundStatus,
                    "Completed",
                    StringComparison.OrdinalIgnoreCase);

            return isPaid
                && !isCancelled
                && !isRefunded;
        }

        // Builds a highest-to-lowest product ranking for one sales period.
        private static List<ProductSalesReportItemViewModel>
            BuildProductSalesRanking(
                List<PetShopProduct> products,
                List<PetShopOrder> periodOrders)
        {
            var salesByProduct = periodOrders
                .SelectMany(o => o.OrderItems)
                .GroupBy(i => i.ProductID)
                .ToDictionary(
                    group => group.Key,
                    group => new
                    {
                        ProductName = group
                            .Select(i => i.ProductName)
                            .FirstOrDefault()
                            ?? "Unknown Product",

                        QuantitySold = group
                            .Sum(i => i.Quantity),

                        Revenue = group
                            .Sum(i => i.LineTotal)
                    });

            List<ProductSalesReportItemViewModel> ranking = products
                .Where(product =>
                    product.ApprovalStatus.Equals(
                        "Approved",
                        StringComparison.OrdinalIgnoreCase)
                    || salesByProduct.ContainsKey(
                        product.ProductID))
                .Select(product =>
                {
                    salesByProduct.TryGetValue(
                        product.ProductID,
                        out var sales);

                    return new ProductSalesReportItemViewModel
                    {
                        ProductID = product.ProductID,
                        ProductName = product.ProductName,
                        ProductCategory = product.ProductCategory,
                        PetType = product.PetType ?? "All",
                        QuantitySold = sales?.QuantitySold ?? 0,
                        Revenue = sales?.Revenue ?? 0,
                        CurrentStock = product.StockQuantity
                    };
                })
                .ToList();

            HashSet<int> existingProductIds = ranking
                .Select(item => item.ProductID)
                .ToHashSet();

            foreach (var sales in salesByProduct)
            {
                if (existingProductIds.Contains(sales.Key))
                {
                    continue;
                }

                ranking.Add(
                    new ProductSalesReportItemViewModel
                    {
                        ProductID = sales.Key,
                        ProductName = sales.Value.ProductName,
                        ProductCategory = "Archived Product",
                        PetType = string.Empty,
                        QuantitySold = sales.Value.QuantitySold,
                        Revenue = sales.Value.Revenue,
                        CurrentStock = 0
                    });
            }

            return ranking
                .OrderByDescending(item => item.QuantitySold)
                .ThenByDescending(item => item.Revenue)
                .ThenBy(item => item.ProductName)
                .ToList();
        }

        // =========================
        // SAMPLE PRODUCT SEED
        // =========================
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
