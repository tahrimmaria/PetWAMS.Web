using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Models;
using PetWAMS.Web.Services;
using PetWAMS.Web.ViewModels;

namespace PetWAMS.Web.Areas.Adopter.Controllers
{
    [Area("Adopter")]
    public class VerificationController : Controller
    {
        private const long MaximumFileSize =
            5 * 1024 * 1024;

        private const string PolicyVersion =
            "1.0";

        private readonly PetWamsDbContext _db;
        private readonly INidProtectionService _nidProtectionService;
        private readonly IWebHostEnvironment _environment;

        public VerificationController(
            PetWamsDbContext db,
            INidProtectionService nidProtectionService,
            IWebHostEnvironment environment)
        {
            _db = db;
            _nidProtectionService = nidProtectionService;
            _environment = environment;
        }

        // Checks whether the current user is a logged-in adopter.
        private bool IsAdopter()
        {
            int? userId =
                HttpContext.Session.GetInt32("UserID");

            string? userType =
                HttpContext.Session.GetString("UserType");

            return userId != null
                && !string.IsNullOrWhiteSpace(userType)
                && userType.Equals(
                    "Adopter",
                    StringComparison.OrdinalIgnoreCase);
        }

        // Displays the verification form or current status.
        [HttpGet]
        public async Task<IActionResult> Index(
            int? petId)
        {
            if (!IsAdopter())
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new { area = "" });
            }

            int userId =
                HttpContext.Session.GetInt32("UserID")!.Value;

            var model =
                new AdopterVerificationViewModel
                {
                    PetID = petId
                };

            if (petId.HasValue)
            {
                Pet? selectedPet =
                    await _db.Pets
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            item => item.PetID == petId.Value);

                if (selectedPet == null)
                {
                    TempData["Error"] =
                        "The selected pet was not found.";

                    return RedirectToAction(
                        "Index",
                        "Pets",
                        new { area = "" });
                }

                bool petAlreadyAdopted =
                    await _db.Adoptions
                        .AsNoTracking()
                        .AnyAsync(item =>
                            item.PetID == petId.Value);

                bool petAlreadyApproved =
                    await _db.AdoptionRequests
                        .AsNoTracking()
                        .AnyAsync(item =>
                            item.PetID == petId.Value
                            && item.Status != null
                            && item.Status.ToLower()
                                == "approved");

                if (petAlreadyAdopted
                    || petAlreadyApproved
                    || string.Equals(
                        selectedPet.Status,
                        "Adopted",
                        StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Error"] =
                        "This pet has already been adopted.";

                    return RedirectToAction(
                        "Index",
                        "Pets",
                        new { area = "" });
                }

                model.SelectedPetName =
                    selectedPet.PetName;
            }

            AdopterVerification? verification =
                await _db.AdopterVerifications
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item => item.UserID == userId);

            if (verification != null)
            {
                PopulateExistingFields(
                    model,
                    verification);
            }
            else
            {
                User? user =
                    await _db.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            item => item.UserID == userId);

                model.FullNameOnNID =
                    user?.FullName ?? string.Empty;
            }

            ViewBag.SuccessMessage =
                TempData["SuccessMessage"]?.ToString();

            return View(model);
        }

        // Accepts the NID submission and creates a pending adoption request.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(12 * 1024 * 1024)]
        public async Task<IActionResult> Index(
            AdopterVerificationViewModel model)
        {
            if (!IsAdopter())
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new { area = "" });
            }

            int userId =
                HttpContext.Session.GetInt32("UserID")!.Value;

            AdopterVerification? existingVerification =
                await _db.AdopterVerifications
                    .FirstOrDefaultAsync(
                        item => item.UserID == userId);

            if (existingVerification != null
                &&
                (
                    string.Equals(
                        existingVerification.VerificationStatus,
                        "Pending",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    string.Equals(
                        existingVerification.VerificationStatus,
                        "Verified",
                        StringComparison.OrdinalIgnoreCase)
                ))
            {
                TempData["SuccessMessage"] =
                    string.Equals(
                        existingVerification.VerificationStatus,
                        "Verified",
                        StringComparison.OrdinalIgnoreCase)
                    ? "Your identity is already verified."
                    : "Your verification is currently under review.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        petId = model.PetID
                    });
            }

            Pet? selectedPet = null;

            if (model.PetID.HasValue)
            {
                if (model.PetID.Value <= 0)
                {
                    ModelState.AddModelError(
                        nameof(model.PetID),
                        "Please select a valid pet.");
                }
                else
                {
                    selectedPet =
                        await _db.Pets
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                item =>
                                    item.PetID
                                    == model.PetID.Value);

                    if (selectedPet == null)
                    {
                        ModelState.AddModelError(
                            nameof(model.PetID),
                            "The selected pet was not found.");
                    }
                    else
                    {
                        model.SelectedPetName =
                            selectedPet.PetName;

                        bool petAlreadyAdopted =
                            await _db.Adoptions
                                .AsNoTracking()
                                .AnyAsync(item =>
                                    item.PetID
                                    == model.PetID.Value);

                        bool petAlreadyApproved =
                            await _db.AdoptionRequests
                                .AsNoTracking()
                                .AnyAsync(item =>
                                    item.PetID
                                        == model.PetID.Value
                                    && item.Status != null
                                    && item.Status.ToLower()
                                        == "approved");

                        if (petAlreadyAdopted
                            || petAlreadyApproved
                            || string.Equals(
                                selectedPet.Status,
                                "Adopted",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            ModelState.AddModelError(
                                nameof(model.PetID),
                                "This pet has already been adopted.");
                        }
                    }
                }
            }

            ValidateDateOfBirth(model);

            await ValidateDocumentAsync(
                model.NIDFrontImage,
                nameof(model.NIDFrontImage),
                true);

            await ValidateDocumentAsync(
                model.NIDBackImage,
                nameof(model.NIDBackImage),
                false);

            string normalizedNid =
                string.Empty;

            string nidHash =
                string.Empty;

            if (!string.IsNullOrWhiteSpace(
                    model.NIDNumber))
            {
                try
                {
                    normalizedNid =
                        _nidProtectionService.NormalizeNid(
                            model.NIDNumber);

                    if (normalizedNid.Length != 10
                        && normalizedNid.Length != 13
                        && normalizedNid.Length != 17)
                    {
                        ModelState.AddModelError(
                            nameof(model.NIDNumber),
                            "NID number must contain 10, 13, or 17 digits.");
                    }
                    else
                    {
                        nidHash =
                            _nidProtectionService.HashNid(
                                normalizedNid);

                        bool nidAlreadyUsed =
                            await _db.AdopterVerifications
                                .AsNoTracking()
                                .AnyAsync(item =>
                                    item.NIDNumberHash == nidHash
                                    && item.UserID != userId);

                        if (nidAlreadyUsed)
                        {
                            ModelState.AddModelError(
                                nameof(model.NIDNumber),
                                "This NID number is already registered.");
                        }
                    }
                }
                catch (ArgumentException)
                {
                    ModelState.AddModelError(
                        nameof(model.NIDNumber),
                        "Please enter a valid NID number.");
                }
            }

            if (!ModelState.IsValid)
            {
                if (existingVerification != null)
                {
                    PopulateExistingFields(
                        model,
                        existingVerification);
                }

                return View(model);
            }

            string? newFrontImagePath = null;
            string? newBackImagePath = null;

            string? oldFrontImagePath =
                existingVerification?.NIDFrontImagePath;

            string? oldBackImagePath =
                existingVerification?.NIDBackImagePath;

            try
            {
                newFrontImagePath =
                    await SaveDocumentAsync(
                        model.NIDFrontImage!,
                        userId);

                if (model.NIDBackImage != null)
                {
                    newBackImagePath =
                        await SaveDocumentAsync(
                            model.NIDBackImage,
                            userId);
                }

                await using var transaction =
                    await _db.Database
                        .BeginTransactionAsync();

                if (existingVerification == null)
                {
                    existingVerification =
                        new AdopterVerification
                        {
                            UserID = userId
                        };

                    _db.AdopterVerifications.Add(
                        existingVerification);
                }

                existingVerification.FullNameOnNID =
                    (model.FullNameOnNID
                        ?? string.Empty).Trim();

                existingVerification.DateOfBirth =
                    model.DateOfBirth!.Value.Date;

                existingVerification.NIDNumberEncrypted =
                    _nidProtectionService.EncryptNid(
                        normalizedNid);

                existingVerification.NIDNumberHash =
                    nidHash;

                existingVerification.NIDLastFour =
                    _nidProtectionService.GetLastFour(
                        normalizedNid);

                existingVerification.NIDFrontImagePath =
                    newFrontImagePath;

                existingVerification.NIDBackImagePath =
                    newBackImagePath;

                existingVerification.VerificationStatus =
                    "Pending";

                existingVerification.AdminRemarks =
                    null;

                existingVerification.VerifiedByAdminUserID =
                    null;

                existingVerification.SubmittedAt =
                    DateTime.Now;

                existingVerification.VerifiedAt =
                    null;

                existingVerification.PolicyAccepted =
                    model.PolicyAccepted;

                existingVerification.PolicyVersion =
                    PolicyVersion;

                existingVerification.PolicyAcceptedAt =
                    model.PolicyAccepted
                    ? DateTime.Now
                    : null;

                AdoptionRequest? adoptionRequest =
                    null;

                if (model.PetID.HasValue
                    && selectedPet != null)
                {
                    adoptionRequest =
                        await _db.AdoptionRequests
                            .FirstOrDefaultAsync(item =>
                                item.UserID == userId
                                && item.PetID
                                    == model.PetID.Value
                                && item.Status != null
                                &&
                                (
                                    item.Status.ToLower()
                                        == "pending"
                                    ||
                                    item.Status.ToLower()
                                        == "approved"
                                ));

                    if (adoptionRequest == null)
                    {
                        adoptionRequest =
                            new AdoptionRequest
                            {
                                UserID = userId,
                                PetID =
                                    model.PetID.Value,
                                RequestDate =
                                    DateTime.Now,
                                Status =
                                    "Pending"
                            };

                        _db.AdoptionRequests.Add(
                            adoptionRequest);
                    }
                }

                // Saves the verification and request before creating the notification.
                await _db.SaveChangesAsync();

                string adopterName =
                    HttpContext.Session
                        .GetString("FullName")
                    ?? "An adopter";

                Notification notification;

                if (adoptionRequest != null)
                {
                    notification =
                        new Notification
                        {
                            Title =
                                "New Adoption Verification Request",

                            Message =
                                adopterName
                                + " submitted identity verification to adopt "
                                + selectedPet!.PetName
                                + ".",

                            Type =
                                "AdoptionRequest",

                            RelatedID =
                                adoptionRequest.RequestID,

                            Link =
                                "/Admin/AdoptionRequests",

                            IsRead =
                                false,

                            CreatedAt =
                                DateTime.Now
                        };
                }
                else
                {
                    notification =
                        new Notification
                        {
                            Title =
                                "New Identity Verification",

                            Message =
                                adopterName
                                + " submitted identity verification.",

                            Type =
                                "AdopterVerification",

                            RelatedID =
                                existingVerification.VerificationID,

                            Link =
                                "/Admin/AdopterVerifications",

                            IsRead =
                                false,

                            CreatedAt =
                                DateTime.Now
                        };
                }

                _db.Notifications.Add(
                    notification);

                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                TryDeleteStoredDocument(
                    oldFrontImagePath);

                TryDeleteStoredDocument(
                    oldBackImagePath);
            }
            catch (DbUpdateException)
            {
                TryDeleteStoredDocument(
                    newFrontImagePath);

                TryDeleteStoredDocument(
                    newBackImagePath);

                ModelState.AddModelError(
                    string.Empty,
                    "The verification and adoption request could not be saved. Please try again.");

                if (existingVerification != null)
                {
                    PopulateExistingFields(
                        model,
                        existingVerification);
                }

                return View(model);
            }
            catch (IOException)
            {
                TryDeleteStoredDocument(
                    newFrontImagePath);

                TryDeleteStoredDocument(
                    newBackImagePath);

                ModelState.AddModelError(
                    string.Empty,
                    "The document could not be saved. Please try again.");

                if (existingVerification != null)
                {
                    PopulateExistingFields(
                        model,
                        existingVerification);
                }

                return View(model);
            }
            catch (UnauthorizedAccessException)
            {
                TryDeleteStoredDocument(
                    newFrontImagePath);

                TryDeleteStoredDocument(
                    newBackImagePath);

                ModelState.AddModelError(
                    string.Empty,
                    "The document storage folder is not accessible.");

                if (existingVerification != null)
                {
                    PopulateExistingFields(
                        model,
                        existingVerification);
                }

                return View(model);
            }

            if (model.PetID.HasValue)
            {
                TempData["SuccessMessage"] =
                    "Your identity verification and adoption request were submitted successfully. Please wait for admin review.";
            }
            else
            {
                TempData["SuccessMessage"] =
                    "Your identity verification was submitted successfully.";
            }

            return RedirectToAction(
                nameof(Index),
                new
                {
                    petId = model.PetID
                });
        }

        // Validates that the adopter is at least 18 years old.
        private void ValidateDateOfBirth(
            AdopterVerificationViewModel model)
        {
            if (!model.DateOfBirth.HasValue)
            {
                return;
            }

            DateTime dateOfBirth =
                model.DateOfBirth.Value.Date;

            if (dateOfBirth > DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.DateOfBirth),
                    "Date of birth cannot be in the future.");

                return;
            }

            if (dateOfBirth
                > DateTime.Today.AddYears(-18))
            {
                ModelState.AddModelError(
                    nameof(model.DateOfBirth),
                    "The adopter must be at least 18 years old.");
            }
        }

        // Validates file size, extension, and actual image signature.
        private async Task ValidateDocumentAsync(
            IFormFile? file,
            string propertyName,
            bool required)
        {
            if (file == null || file.Length == 0)
            {
                if (required)
                {
                    ModelState.AddModelError(
                        propertyName,
                        "This document image is required.");
                }

                return;
            }

            if (file.Length > MaximumFileSize)
            {
                ModelState.AddModelError(
                    propertyName,
                    "The image size cannot exceed 5 MB.");

                return;
            }

            string extension =
                Path.GetExtension(file.FileName)
                    .ToLowerInvariant();

            bool allowedExtension =
                extension == ".jpg"
                || extension == ".jpeg"
                || extension == ".png"
                || extension == ".webp";

            if (!allowedExtension)
            {
                ModelState.AddModelError(
                    propertyName,
                    "Only JPG, JPEG, PNG, and WEBP images are allowed.");

                return;
            }

            bool validImage =
                await HasValidImageSignatureAsync(
                    file,
                    extension);

            if (!validImage)
            {
                ModelState.AddModelError(
                    propertyName,
                    "The uploaded file is not a valid image.");
            }
        }

        // Checks the real file header instead of trusting the file name.
        private static async Task<bool>
            HasValidImageSignatureAsync(
                IFormFile file,
                string extension)
        {
            byte[] header =
                new byte[12];

            await using Stream stream =
                file.OpenReadStream();

            int bytesRead =
                await stream.ReadAsync(
                    header.AsMemory(
                        0,
                        header.Length));

            bool isJpeg =
                bytesRead >= 3
                && header[0] == 0xFF
                && header[1] == 0xD8
                && header[2] == 0xFF;

            bool isPng =
                bytesRead >= 8
                && header[0] == 0x89
                && header[1] == 0x50
                && header[2] == 0x4E
                && header[3] == 0x47
                && header[4] == 0x0D
                && header[5] == 0x0A
                && header[6] == 0x1A
                && header[7] == 0x0A;

            bool isWebP =
                bytesRead >= 12
                && header[0] == 0x52
                && header[1] == 0x49
                && header[2] == 0x46
                && header[3] == 0x46
                && header[8] == 0x57
                && header[9] == 0x45
                && header[10] == 0x42
                && header[11] == 0x50;

            return extension switch
            {
                ".jpg" => isJpeg,
                ".jpeg" => isJpeg,
                ".png" => isPng,
                ".webp" => isWebP,
                _ => false
            };
        }

        // Stores the document outside wwwroot.
        private async Task<string> SaveDocumentAsync(
            IFormFile file,
            int userId)
        {
            string rootDirectory =
                GetDocumentRootDirectory();

            string userDirectory =
                Path.Combine(
                    rootDirectory,
                    userId.ToString());

            Directory.CreateDirectory(
                userDirectory);

            string extension =
                Path.GetExtension(file.FileName)
                    .ToLowerInvariant();

            string fileName =
                $"{Guid.NewGuid():N}{extension}";

            string fullPath =
                Path.Combine(
                    userDirectory,
                    fileName);

            await using var fileStream =
                new FileStream(
                    fullPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None);

            await file.CopyToAsync(
                fileStream);

            return $"{userId}/{fileName}";
        }

        // Returns the private NID document directory.
        private string GetDocumentRootDirectory()
        {
            return Path.Combine(
                _environment.ContentRootPath,
                "App_Data",
                "AdopterVerificationDocuments");
        }

        // Safely deletes a stored document.
        private void TryDeleteStoredDocument(
            string? relativePath)
        {
            try
            {
                DeleteStoredDocument(
                    relativePath);
            }
            catch (IOException)
            {
                // A failed cleanup must not break the completed database operation.
            }
            catch (UnauthorizedAccessException)
            {
                // A failed cleanup must not break the completed database operation.
            }
        }

        // Deletes an old private document after a successful replacement.
        private void DeleteStoredDocument(
            string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(
                    relativePath))
            {
                return;
            }

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
                return;
            }

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }

        // Adds current verification information to the view model.
        private static void PopulateExistingFields(
            AdopterVerificationViewModel model,
            AdopterVerification verification)
        {
            model.HasExistingVerification =
                true;

            model.FullNameOnNID =
                verification.FullNameOnNID;

            model.DateOfBirth =
                verification.DateOfBirth;

            model.CurrentStatus =
                verification.VerificationStatus;

            model.MaskedNID =
                verification.MaskedNID;

            model.AdminRemarks =
                verification.AdminRemarks;

            model.SubmittedAt =
                verification.SubmittedAt;
        }
    }
}