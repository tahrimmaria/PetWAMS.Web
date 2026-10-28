using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Models;

namespace PetWAMS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UsersController : Controller
    {
        private readonly PetWamsDbContext _db;
        private readonly PasswordHasher<User> _passwordHasher;

        public UsersController(PetWamsDbContext db)
        {
            _db = db;
            _passwordHasher = new PasswordHasher<User>();
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("UserType") == "Admin";
        }


        // =========================
        // USER LIST
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

            var users = await _db.Users
                .Include(u => u.Shelter)
                .OrderBy(u => u.UserID)
                .ToListAsync();

            return View(users);
        }


        // =========================
        // CREATE USER
        // =========================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            ViewBag.Shelters = await _db.Shelters
                .Where(s => s.ApprovalStatus == "Approved")
                .OrderBy(s => s.ShelterName)
                .ToListAsync();

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User model)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            bool emailExists = await _db.Users
                .AnyAsync(u => u.Email == model.Email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email is already registered."
                );
            }

            if (model.UserType == "Staff")
            {
                if (model.ShelterID == null)
                {
                    ModelState.AddModelError(
                        "ShelterID",
                        "Please select a shelter for this staff member."
                    );
                }
                else
                {
                    bool shelterExists = await _db.Shelters
                        .AnyAsync(s =>
                            s.ShelterID == model.ShelterID &&
                            s.ApprovalStatus == "Approved");

                    if (!shelterExists)
                    {
                        ModelState.AddModelError(
                            "ShelterID",
                            "Selected shelter is not approved or was not found."
                        );
                    }
                }
            }
            else
            {
                model.ShelterID = null;
            }

            if (ModelState.IsValid)
            {
                // Admin create করলে direct Approved
                model.ApprovalStatus = "Approved";

                // Password পরে Reset Password থেকে set করা যাবে
                model.PasswordHash = null;

                _db.Users.Add(model);

                await _db.SaveChangesAsync();

                TempData["Success"] =
                    "User created successfully. Please set the user's password.";

                return RedirectToAction("Index");
            }

            ViewBag.Shelters = await _db.Shelters
                .Where(s => s.ApprovalStatus == "Approved")
                .OrderBy(s => s.ShelterName)
                .ToListAsync();

            return View(model);
        }


        // =========================
        // EDIT USER
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

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.UserID == id);

            if (user == null)
            {
                return NotFound();
            }

            ViewBag.Shelters = await _db.Shelters
                .Where(s =>
                    s.ApprovalStatus == "Approved" ||
                    s.ShelterID == user.ShelterID)
                .OrderBy(s => s.ShelterName)
                .ToListAsync();

            return View(user);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(User model)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            var user = await _db.Users
                .FirstOrDefaultAsync(u =>
                    u.UserID == model.UserID);

            if (user == null)
            {
                return NotFound();
            }

            bool emailExists = await _db.Users
                .AnyAsync(u =>
                    u.Email == model.Email &&
                    u.UserID != model.UserID);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email is already registered."
                );
            }

            if (model.UserType == "Staff")
            {
                if (model.ShelterID == null)
                {
                    ModelState.AddModelError(
                        "ShelterID",
                        "Please select a shelter for this staff member."
                    );
                }
                else
                {
                    bool shelterExists = await _db.Shelters
                        .AnyAsync(s =>
                            s.ShelterID == model.ShelterID);

                    if (!shelterExists)
                    {
                        ModelState.AddModelError(
                            "ShelterID",
                            "Selected shelter was not found."
                        );
                    }
                }
            }
            else
            {
                model.ShelterID = null;
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Shelters = await _db.Shelters
                    .Where(s =>
                        s.ApprovalStatus == "Approved" ||
                        s.ShelterID == model.ShelterID)
                    .OrderBy(s => s.ShelterName)
                    .ToListAsync();

                return View(model);
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.Phone = model.Phone;
            user.Address = model.Address;
            user.UserType = model.UserType;
            user.ShelterID = model.ShelterID;

            // PasswordHash এখানে change হবে না

            if (user.UserType != "Staff")
            {
                user.ApprovalStatus = "Approved";
            }

            await _db.SaveChangesAsync();

            TempData["Success"] =
                "User information updated successfully.";

            return RedirectToAction("Index");
        }


        // =========================
        // RESET PASSWORD GET
        // =========================
        [HttpGet]
        public async Task<IActionResult> ResetPassword(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            var user = await _db.Users
                .FirstOrDefaultAsync(u =>
                    u.UserID == id);

            if (user == null)
            {
                return NotFound();
            }

            // Admin-এর admin123 system change হবে না
            if (user.UserType == "Admin")
            {
                TempData["Error"] =
                    "Admin password is managed separately.";

                return RedirectToAction("Index");
            }

            return View(user);
        }


        // =========================
        // RESET PASSWORD POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            int id,
            string newPassword,
            string confirmPassword)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            var user = await _db.Users
                .FirstOrDefaultAsync(u =>
                    u.UserID == id);

            if (user == null)
            {
                return NotFound();
            }

            if (user.UserType == "Admin")
            {
                TempData["Error"] =
                    "Admin password cannot be changed here.";

                return RedirectToAction("Index");
            }


            // PASSWORD REQUIRED
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                ViewBag.Error =
                    "New password is required.";

                return View(user);
            }


            // MINIMUM LENGTH
            if (newPassword.Length < 6)
            {
                ViewBag.Error =
                    "Password must be at least 6 characters.";

                return View(user);
            }


            // CONFIRM PASSWORD
            if (newPassword != confirmPassword)
            {
                ViewBag.Error =
                    "New password and confirm password do not match.";

                return View(user);
            }


            // HASH PASSWORD
            user.PasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    newPassword
                );


            await _db.SaveChangesAsync();


            TempData["Success"] =
                $"Password has been set successfully for {user.FullName}.";


            return RedirectToAction("Index");
        }


        // =========================
        // APPROVE STAFF
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            var user = await _db.Users
                .Include(u => u.Shelter)
                .FirstOrDefaultAsync(u =>
                    u.UserID == id);

            if (user == null)
            {
                return NotFound();
            }

            if (user.UserType != "Staff")
            {
                TempData["Error"] =
                    "Only staff accounts require approval.";

                return RedirectToAction("Index");
            }

            if (user.ShelterID == null ||
                user.Shelter == null)
            {
                TempData["Error"] =
                    "This staff account has no shelter assigned.";

                return RedirectToAction("Index");
            }


            // STAFF APPROVE
            user.ApprovalStatus = "Approved";


            // NEW PENDING / REJECTED SHELTER
            // হলে Shelter-ও approve
            if (user.Shelter.ApprovalStatus == "Pending" ||
                user.Shelter.ApprovalStatus == "Rejected")
            {
                user.Shelter.ApprovalStatus =
                    "Approved";
            }


            await _db.SaveChangesAsync();


            TempData["Success"] =
                $"{user.FullName} and the related shelter have been approved.";


            return RedirectToAction("Index");
        }


        // =========================
        // REJECT STAFF
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "Account",
                    new { area = "" }
                );
            }

            var user = await _db.Users
                .Include(u => u.Shelter)
                .FirstOrDefaultAsync(u =>
                    u.UserID == id);

            if (user == null)
            {
                return NotFound();
            }

            if (user.UserType != "Staff")
            {
                TempData["Error"] =
                    "Only staff accounts can be rejected.";

                return RedirectToAction("Index");
            }


            // NEW SHELTER REGISTRATION
            if (user.Shelter != null &&
                user.Shelter.ApprovalStatus == "Pending")
            {
                user.Shelter.ApprovalStatus =
                    "Rejected";
            }


            // STAFF REJECT
            user.ApprovalStatus =
                "Rejected";


            await _db.SaveChangesAsync();


            TempData["Success"] =
                $"{user.FullName}'s registration has been rejected.";


            return RedirectToAction("Index");
        }


        // =========================
        // DELETE USER
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

            var user = await _db.Users
                .FirstOrDefaultAsync(u =>
                    u.UserID == id);

            if (user == null)
            {
                return NotFound();
            }


            bool hasRelatedData =
                await _db.AdoptionRequests
                    .AnyAsync(x =>
                        x.UserID == id) ||

                await _db.Adoptions
                    .AnyAsync(x =>
                        x.UserID == id) ||

                await _db.LostPets
                    .AnyAsync(x =>
                        x.UserID == id) ||

                await _db.FoundPets
                    .AnyAsync(x =>
                        x.UserID == id);


            if (hasRelatedData)
            {
                TempData["Error"] =
                    "This user cannot be deleted because related records exist.";

                return RedirectToAction("Index");
            }


            _db.Users.Remove(user);

            await _db.SaveChangesAsync();


            return RedirectToAction("Index");
        }
    }
}