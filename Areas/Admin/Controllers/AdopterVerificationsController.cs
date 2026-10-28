using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Models;
using PetWAMS.Web.Services;
using PetWAMS.Web.ViewModels;

namespace PetWAMS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdopterVerificationsController : Controller
    {
        private readonly PetWamsDbContext _db;
        private readonly INidProtectionService _nidProtectionService;
        private readonly IWebHostEnvironment _environment;

        public AdopterVerificationsController(
            PetWamsDbContext db,
            INidProtectionService nidProtectionService,
            IWebHostEnvironment environment)
        {
            _db = db;
            _nidProtectionService = nidProtectionService;
            _environment = environment;
        }

        // Checks whether the current user is a logged-in admin.
        private bool IsAdmin()
        {
            int? userId =
                HttpContext.Session.GetInt32("UserID");

            string? userType =
                HttpContext.Session.GetString("UserType");

            return userId != null
                && !string.IsNullOrWhiteSpace(userType)
                && userType.Equals(
                    "Admin",
                    StringComparison.OrdinalIgnoreCase);
        }

        // Redirects unauthorized users to the unified sign-in page.
        private IActionResult RedirectToSignIn()
        {
            return RedirectToAction(
                "SignIn",
                "Account",
                new
                {
                    area = ""
                });
        }

        // Redirects back to the verification details page.
        private IActionResult RedirectToDetails(
            int verificationId,
            int? requestId)
        {
            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = verificationId,
                    requestId
                });
        }

        // Displays all adopter verification submissions.
        [HttpGet]
        public async Task<IActionResult> Index(
            string? status)
        {
            if (!IsAdmin())
            {
                return RedirectToSignIn();
            }

            string selectedStatus =
                NormalizeStatusFilter(status);

            IQueryable<AdopterVerification> query =
                _db.AdopterVerifications
                    .AsNoTracking()
                    .Include(item => item.User);

            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                query = query.Where(item =>
                    item.VerificationStatus
                        == selectedStatus);
            }

            List<AdopterVerification> verifications =
                await query
                    .OrderByDescending(
                        item => item.SubmittedAt)
                    .ToListAsync();

            List<AdminAdopterVerificationViewModel> models =
                verifications
                    .Select(CreateListViewModel)
                    .ToList();

            ViewBag.SelectedStatus =
                selectedStatus;

            ViewBag.TotalCount =
                await _db.AdopterVerifications
                    .AsNoTracking()
                    .CountAsync();

            ViewBag.PendingCount =
                await _db.AdopterVerifications
                    .AsNoTracking()
                    .CountAsync(item =>
                        item.VerificationStatus
                            == "Pending");

            ViewBag.VerifiedCount =
                await _db.AdopterVerifications
                    .AsNoTracking()
                    .CountAsync(item =>
                        item.VerificationStatus
                            == "Verified");

            ViewBag.RejectedCount =
                await _db.AdopterVerifications
                    .AsNoTracking()
                    .CountAsync(item =>
                        item.VerificationStatus
                            == "Rejected");

            ViewBag.SuccessMessage =
                TempData["SuccessMessage"]?.ToString();

            ViewBag.ErrorMessage =
                TempData["ErrorMessage"]?.ToString();

            return View(models);
        }

        // Displays the verification and linked adoption request.
        [HttpGet]
        public async Task<IActionResult> Details(
            int id,
            int? requestId)
        {
            if (!IsAdmin())
            {
                return RedirectToSignIn();
            }

            AdopterVerification? verification =
                await _db.AdopterVerifications
                    .AsNoTracking()
                    .Include(item => item.User)
                    .FirstOrDefaultAsync(
                        item =>
                            item.VerificationID == id);

            if (verification == null)
            {
                return NotFound();
            }

            AdoptionRequest? adoptionRequest =
                await FindLinkedRequestAsync(
                    verification.UserID,
                    requestId,
                    true);

            if (requestId.HasValue
                && adoptionRequest == null)
            {
                ViewBag.LinkError =
                    "The selected adoption request is not linked to this adopter.";
            }

            string fullNid;

            try
            {
                fullNid =
                    _nidProtectionService.DecryptNid(
                        verification.NIDNumberEncrypted);
            }
            catch (CryptographicException)
            {
                fullNid =
                    "Unable to decrypt";

                ViewBag.DecryptionError =
                    "The saved NID could not be decrypted with the current protection key.";
            }

            AdminAdopterVerificationViewModel model =
                CreateDetailsViewModel(
                    verification,
                    fullNid);

            ViewBag.RequestID =
                adoptionRequest?.RequestID;

            ViewBag.PetID =
                adoptionRequest?.PetID;

            ViewBag.PetName =
                adoptionRequest?.Pet?.PetName;

            ViewBag.RequestStatus =
                adoptionRequest?.Status;

            ViewBag.RequestDate =
                adoptionRequest?.RequestDate;

            ViewBag.HasLinkedAdoptionRequest =
                adoptionRequest != null;

            ViewBag.SuccessMessage =
                TempData["SuccessMessage"]?.ToString();

            ViewBag.ErrorMessage =
                TempData["ErrorMessage"]?.ToString();

            return View(model);
        }

        // Opens a private NID image for an authorized admin.
        [HttpGet]
        [ResponseCache(
            NoStore = true,
            Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Document(
            int id,
            string side)
        {
            if (!IsAdmin())
            {
                return RedirectToSignIn();
            }

            AdopterVerification? verification =
                await _db.AdopterVerifications
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item =>
                            item.VerificationID == id);

            if (verification == null)
            {
                return NotFound();
            }

            string? relativePath =
                side.Equals(
                    "front",
                    StringComparison.OrdinalIgnoreCase)
                    ? verification.NIDFrontImagePath
                    : side.Equals(
                        "back",
                        StringComparison.OrdinalIgnoreCase)
                        ? verification.NIDBackImagePath
                        : null;

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return NotFound();
            }

            string? fullPath =
                GetSafeDocumentPath(
                    relativePath);

            if (string.IsNullOrWhiteSpace(fullPath)
                || !System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }

            string contentType =
                GetImageContentType(fullPath);

            Response.Headers.CacheControl =
                "no-store, no-cache, must-revalidate";

            Response.Headers.Pragma =
                "no-cache";

            return PhysicalFile(
                fullPath,
                contentType);
        }

        // Approves identity and the linked adoption request.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(
            AdminVerificationDecisionViewModel model,
            int? requestId)
        {
            if (!IsAdmin())
            {
                return RedirectToSignIn();
            }

            if (model.VerificationID <= 0)
            {
                return BadRequest();
            }

            if (!string.IsNullOrWhiteSpace(model.AdminRemarks)
                && model.AdminRemarks.Trim().Length > 500)
            {
                TempData["ErrorMessage"] =
                    "Admin remarks cannot exceed 500 characters.";

                return RedirectToDetails(
                    model.VerificationID,
                    requestId);
            }

            AdopterVerification? verification =
                await _db.AdopterVerifications
                    .FirstOrDefaultAsync(item =>
                        item.VerificationID
                            == model.VerificationID);

            if (verification == null)
            {
                return NotFound();
            }

            string verificationStatus =
                verification.VerificationStatus
                ?? string.Empty;

            bool verificationPending =
                verificationStatus.Equals(
                    "Pending",
                    StringComparison.OrdinalIgnoreCase);

            bool verificationAlreadyVerified =
                verificationStatus.Equals(
                    "Verified",
                    StringComparison.OrdinalIgnoreCase);

            if (!verificationPending
                && !verificationAlreadyVerified)
            {
                TempData["ErrorMessage"] =
                    "Only pending or verified identity records can be used for adoption approval.";

                return RedirectToDetails(
                    model.VerificationID,
                    requestId);
            }

            if (!verification.PolicyAccepted)
            {
                TempData["ErrorMessage"] =
                    "The adopter has not accepted the adoption policy.";

                return RedirectToDetails(
                    model.VerificationID,
                    requestId);
            }

            AdoptionRequest? adoptionRequest =
                await FindLinkedRequestAsync(
                    verification.UserID,
                    requestId,
                    false);

            if (requestId.HasValue
                && adoptionRequest == null)
            {
                TempData["ErrorMessage"] =
                    "The selected adoption request is not linked to this adopter.";

                return RedirectToDetails(
                    model.VerificationID,
                    requestId);
            }

            string cleanRemarks =
                string.IsNullOrWhiteSpace(model.AdminRemarks)
                    ? "Identity documents verified by admin."
                    : model.AdminRemarks.Trim();

            int? adminUserId =
                HttpContext.Session.GetInt32("UserID");

            // Allows identity-only approval when no adoption request exists.
            if (adoptionRequest == null)
            {
                if (!verificationPending)
                {
                    TempData["ErrorMessage"] =
                        "This identity has already been verified.";

                    return RedirectToDetails(
                        model.VerificationID,
                        requestId);
                }

                verification.VerificationStatus =
                    "Verified";

                verification.AdminRemarks =
                    cleanRemarks;

                verification.VerifiedByAdminUserID =
                    adminUserId;

                verification.VerifiedAt =
                    DateTime.Now;

                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "The adopter identity was verified successfully.";

                return RedirectToDetails(
                    model.VerificationID,
                    null);
            }

            string requestStatus =
                adoptionRequest.Status
                ?? string.Empty;

            if (!requestStatus.Equals(
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "Only pending adoption requests can be approved.";

                return RedirectToDetails(
                    model.VerificationID,
                    adoptionRequest.RequestID);
            }

            if (!adoptionRequest.UserID.HasValue
                || adoptionRequest.UserID.Value
                    != verification.UserID)
            {
                TempData["ErrorMessage"] =
                    "The adoption request does not belong to this adopter.";

                return RedirectToDetails(
                    model.VerificationID,
                    adoptionRequest.RequestID);
            }

            if (!adoptionRequest.PetID.HasValue
                || adoptionRequest.Pet == null)
            {
                TempData["ErrorMessage"] =
                    "The pet linked to this adoption request was not found.";

                return RedirectToDetails(
                    model.VerificationID,
                    adoptionRequest.RequestID);
            }

            bool adoptionAlreadyExists =
                await _db.Adoptions
                    .AnyAsync(item =>
                        item.PetID
                            == adoptionRequest.PetID.Value);

            bool anotherRequestAlreadyApproved =
                await _db.AdoptionRequests
                    .AnyAsync(item =>
                        item.PetID
                            == adoptionRequest.PetID
                        && item.RequestID
                            != adoptionRequest.RequestID
                        && item.Status != null
                        && item.Status.ToLower()
                            == "approved");

            bool petStatusAdopted =
                !string.IsNullOrWhiteSpace(
                    adoptionRequest.Pet.Status)
                && adoptionRequest.Pet.Status.Equals(
                    "Adopted",
                    StringComparison.OrdinalIgnoreCase);

            if (adoptionAlreadyExists
                || anotherRequestAlreadyApproved
                || petStatusAdopted)
            {
                TempData["ErrorMessage"] =
                    "This pet has already been adopted or approved for another adopter.";

                return RedirectToDetails(
                    model.VerificationID,
                    adoptionRequest.RequestID);
            }

            await using var transaction =
                await _db.Database
                    .BeginTransactionAsync();

            try
            {
                if (verificationPending)
                {
                    verification.VerificationStatus =
                        "Verified";

                    verification.AdminRemarks =
                        cleanRemarks;

                    verification.VerifiedByAdminUserID =
                        adminUserId;

                    verification.VerifiedAt =
                        DateTime.Now;
                }

                adoptionRequest.Status =
                    "Approved";

                adoptionRequest.Pet.Status =
                    "Adopted";

                var adoption =
                    new Adoption
                    {
                        PetID =
                            adoptionRequest.PetID.Value,

                        UserID =
                            verification.UserID,

                        AdoptionDate =
                            DateTime.Now
                    };

                _db.Adoptions.Add(
                    adoption);

                List<AdoptionRequest> otherRequests =
                    await _db.AdoptionRequests
                        .Where(item =>
                            item.PetID
                                == adoptionRequest.PetID
                            && item.RequestID
                                != adoptionRequest.RequestID
                            && item.Status != null
                            && item.Status.ToLower()
                                == "pending")
                        .ToListAsync();

                foreach (AdoptionRequest otherRequest
                    in otherRequests)
                {
                    otherRequest.Status =
                        "Rejected";
                }

                await _db.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            TempData["Success"] =
                "Identity verified and adoption request approved successfully.";

            return RedirectToAction(
                "Index",
                "AdoptionRequests",
                new
                {
                    area = "Admin"
                });
        }

        // Rejects identity and the linked adoption request.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            AdminVerificationDecisionViewModel model,
            int? requestId)
        {
            if (!IsAdmin())
            {
                return RedirectToSignIn();
            }

            if (model.VerificationID <= 0)
            {
                return BadRequest();
            }

            if (string.IsNullOrWhiteSpace(
                model.AdminRemarks))
            {
                TempData["ErrorMessage"] =
                    "A rejection reason is required.";

                return RedirectToDetails(
                    model.VerificationID,
                    requestId);
            }

            string cleanRemarks =
                model.AdminRemarks.Trim();

            if (cleanRemarks.Length > 500)
            {
                TempData["ErrorMessage"] =
                    "Admin remarks cannot exceed 500 characters.";

                return RedirectToDetails(
                    model.VerificationID,
                    requestId);
            }

            AdopterVerification? verification =
                await _db.AdopterVerifications
                    .FirstOrDefaultAsync(item =>
                        item.VerificationID
                            == model.VerificationID);

            if (verification == null)
            {
                return NotFound();
            }

            string verificationStatus =
                verification.VerificationStatus
                ?? string.Empty;

            bool verificationPending =
                verificationStatus.Equals(
                    "Pending",
                    StringComparison.OrdinalIgnoreCase);

            bool verificationAlreadyVerified =
                verificationStatus.Equals(
                    "Verified",
                    StringComparison.OrdinalIgnoreCase);

            if (!verificationPending
                && !verificationAlreadyVerified)
            {
                TempData["ErrorMessage"] =
                    "This verification record cannot be rejected.";

                return RedirectToDetails(
                    model.VerificationID,
                    requestId);
            }

            AdoptionRequest? adoptionRequest =
                await FindLinkedRequestAsync(
                    verification.UserID,
                    requestId,
                    false);

            if (requestId.HasValue
                && adoptionRequest == null)
            {
                TempData["ErrorMessage"] =
                    "The selected adoption request is not linked to this adopter.";

                return RedirectToDetails(
                    model.VerificationID,
                    requestId);
            }

            int? adminUserId =
                HttpContext.Session.GetInt32("UserID");

            // Allows identity-only rejection when no adoption request exists.
            if (adoptionRequest == null)
            {
                if (!verificationPending)
                {
                    TempData["ErrorMessage"] =
                        "A verified identity cannot be rejected without a linked pending adoption request.";

                    return RedirectToDetails(
                        model.VerificationID,
                        requestId);
                }

                verification.VerificationStatus =
                    "Rejected";

                verification.AdminRemarks =
                    cleanRemarks;

                verification.VerifiedByAdminUserID =
                    adminUserId;

                verification.VerifiedAt =
                    DateTime.Now;

                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "The verification submission was rejected.";

                return RedirectToDetails(
                    model.VerificationID,
                    null);
            }

            string requestStatus =
                adoptionRequest.Status
                ?? string.Empty;

            if (!requestStatus.Equals(
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "Only pending adoption requests can be rejected.";

                return RedirectToDetails(
                    model.VerificationID,
                    adoptionRequest.RequestID);
            }

            await using var transaction =
                await _db.Database
                    .BeginTransactionAsync();

            try
            {
                if (verificationPending)
                {
                    verification.VerificationStatus =
                        "Rejected";

                    verification.AdminRemarks =
                        cleanRemarks;

                    verification.VerifiedByAdminUserID =
                        adminUserId;

                    verification.VerifiedAt =
                        DateTime.Now;
                }

                adoptionRequest.Status =
                    "Rejected";

                await _db.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            TempData["Success"] =
                verificationPending
                    ? "Identity verification and adoption request were rejected."
                    : "The adoption request was rejected.";

            return RedirectToAction(
                "Index",
                "AdoptionRequests",
                new
                {
                    area = "Admin"
                });
        }

        // Finds the selected request or the latest pending request.
        private async Task<AdoptionRequest?>
            FindLinkedRequestAsync(
                int userId,
                int? requestId,
                bool useNoTracking)
        {
            IQueryable<AdoptionRequest> query =
                _db.AdoptionRequests
                    .Include(item => item.Pet);

            if (useNoTracking)
            {
                query =
                    query.AsNoTracking();
            }

            if (requestId.HasValue)
            {
                return await query
                    .FirstOrDefaultAsync(item =>
                        item.RequestID
                            == requestId.Value
                        && item.UserID
                            == userId);
            }

            return await query
                .Where(item =>
                    item.UserID == userId
                    && item.Status != null
                    && item.Status.ToLower()
                        == "pending")
                .OrderByDescending(
                    item => item.RequestID)
                .FirstOrDefaultAsync();
        }

        // Creates a safe list model without decrypting the full NID.
        private static AdminAdopterVerificationViewModel
            CreateListViewModel(
                AdopterVerification verification)
        {
            return new AdminAdopterVerificationViewModel
            {
                VerificationID =
                    verification.VerificationID,

                UserID =
                    verification.UserID,

                AdopterName =
                    verification.User?.FullName
                    ?? "Unknown Adopter",

                AdopterEmail =
                    verification.User?.Email
                    ?? string.Empty,

                AdopterPhone =
                    verification.User?.Phone
                    ?? string.Empty,

                FullNameOnNID =
                    verification.FullNameOnNID,

                DateOfBirth =
                    verification.DateOfBirth,

                MaskedNID =
                    verification.MaskedNID,

                VerificationStatus =
                    verification.VerificationStatus,

                AdminRemarks =
                    verification.AdminRemarks
                    ?? string.Empty,

                SubmittedAt =
                    verification.SubmittedAt,

                VerifiedAt =
                    verification.VerifiedAt,

                PolicyAccepted =
                    verification.PolicyAccepted,

                PolicyVersion =
                    verification.PolicyVersion,

                HasFrontImage =
                    !string.IsNullOrWhiteSpace(
                        verification.NIDFrontImagePath),

                HasBackImage =
                    !string.IsNullOrWhiteSpace(
                        verification.NIDBackImagePath)
            };
        }

        // Creates the details model containing the decrypted NID.
        private static AdminAdopterVerificationViewModel
            CreateDetailsViewModel(
                AdopterVerification verification,
                string fullNid)
        {
            AdminAdopterVerificationViewModel model =
                CreateListViewModel(
                    verification);

            model.FullNID =
                fullNid;

            return model;
        }

        // Allows only supported verification status filters.
        private static string NormalizeStatusFilter(
            string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return string.Empty;
            }

            if (status.Equals(
                "Pending",
                StringComparison.OrdinalIgnoreCase))
            {
                return "Pending";
            }

            if (status.Equals(
                "Verified",
                StringComparison.OrdinalIgnoreCase))
            {
                return "Verified";
            }

            if (status.Equals(
                "Rejected",
                StringComparison.OrdinalIgnoreCase))
            {
                return "Rejected";
            }

            return string.Empty;
        }

        // Returns the private NID document root directory.
        private string GetDocumentRootDirectory()
        {
            return Path.Combine(
                _environment.ContentRootPath,
                "App_Data",
                "AdopterVerificationDocuments");
        }

        // Resolves a safe path and blocks path traversal.
        private string? GetSafeDocumentPath(
            string relativePath)
        {
            string rootDirectory =
                Path.GetFullPath(
                    GetDocumentRootDirectory());

            string safeRelativePath =
                relativePath.Replace(
                    '/',
                    Path.DirectorySeparatorChar);

            string fullPath =
                Path.GetFullPath(
                    Path.Combine(
                        rootDirectory,
                        safeRelativePath));

            string allowedPrefix =
                rootDirectory
                + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(
                allowedPrefix,
                StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return fullPath;
        }

        // Returns the content type for a stored image.
        private static string GetImageContentType(
            string fullPath)
        {
            string extension =
                Path.GetExtension(fullPath)
                    .ToLowerInvariant();

            return extension switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }
    }
}