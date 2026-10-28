using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Helpers;
using PetWAMS.Web.Models;

namespace PetWAMS.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly PetWamsDbContext _db;

        public AccountController(PetWamsDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult SignIn()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn(
            string email,
            string password,
            bool rememberMe = false)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Email and password are required.";
                return View();
            }

            string normalizedEmail = email.Trim().ToLower();
            string givenPassword = password.Trim();

            var user = await _db.Users
                .FirstOrDefaultAsync(u =>
                    u.Email != null &&
                    u.Email.ToLower() == normalizedEmail);

            if (user == null)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            bool passwordOk = false;

            string userType = user.UserType ?? "";

            /*
                DEMO STAFF LOGIN FIX:
                Staff/Shelter account hole password hash check korbe na.
                staff123 dile direct login hobe.
            */
            if (
                (
                    userType.Equals("Staff", StringComparison.OrdinalIgnoreCase) ||
                    userType.Equals("Shelter", StringComparison.OrdinalIgnoreCase)
                )
                &&
                givenPassword == "staff123"
            )
            {
                passwordOk = true;

                user.UserType = "Staff";
                user.ApprovalStatus = "Approved";

                if (!string.IsNullOrWhiteSpace(user.PasswordHash))
                {
                    user.PasswordHash = PasswordHelper.HashPassword("staff123");
                }

                await _db.SaveChangesAsync();
            }

            /*
                Tamim special fallback:
                Tamim-er jonno staff123 ba tamim123 duita accept korbe.
            */
            if (!passwordOk &&
                normalizedEmail == "tamim@gmail.com" &&
                (
                    givenPassword == "staff123" ||
                    givenPassword == "tamim123"
                ))
            {
                passwordOk = true;

                user.UserType = "Staff";
                user.ApprovalStatus = "Approved";
                user.ShelterID = 4;
                user.PasswordHash = PasswordHelper.HashPassword("staff123");

                await _db.SaveChangesAsync();
            }

            /*
                Normal hashed password login:
                Admin/Adopter er jonno.
            */
            if (!passwordOk &&
                !string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                try
                {
                    passwordOk = PasswordHelper.VerifyPassword(givenPassword, user.PasswordHash);
                }
                catch
                {
                    passwordOk = false;
                }
            }

            /*
                Demo admin fallback.
            */
            if (!passwordOk &&
                userType.Equals("Admin", StringComparison.OrdinalIgnoreCase) &&
                givenPassword == "admin123")
            {
                passwordOk = true;
                user.PasswordHash = PasswordHelper.HashPassword("admin123");
                user.ApprovalStatus = "Approved";

                await _db.SaveChangesAsync();
            }

            if (!passwordOk)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            if (!string.IsNullOrWhiteSpace(user.ApprovalStatus) &&
                user.ApprovalStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Error = "Your account is pending approval.";
                return View();
            }

            if (!string.IsNullOrWhiteSpace(user.ApprovalStatus) &&
                user.ApprovalStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Error = "Your account has been rejected.";
                return View();
            }

            HttpContext.Session.SetInt32("UserID", user.UserID);
            HttpContext.Session.SetString("FullName", user.FullName ?? "");
            HttpContext.Session.SetString("Email", user.Email ?? "");
            HttpContext.Session.SetString("Phone", user.Phone ?? "");
            HttpContext.Session.SetString("Address", user.Address ?? "");
            HttpContext.Session.SetString("UserType", user.UserType ?? "");
            HttpContext.Session.SetString("ApprovalStatus", user.ApprovalStatus ?? "");

            if ((user.UserType ?? "").Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            if ((user.UserType ?? "").Equals("Staff", StringComparison.OrdinalIgnoreCase) ||
                (user.UserType ?? "").Equals("Shelter", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Shelter" });
            }

            if ((user.UserType ?? "").Equals("Adopter", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Adopter" });
            }

            ViewBag.Error = "Invalid user role.";
            return View();
        }

        [HttpGet]
        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(
            string fullName,
            string email,
            string phone,
            string address,
            string password,
            string confirmPassword,
            string userType = "Adopter")
        {
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(phone) ||
                string.IsNullOrWhiteSpace(address) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.Error = "All fields are required.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Password and confirm password do not match.";
                return View();
            }

            string normalizedEmail = email.Trim().ToLower();
            string normalizedPhone = phone.Trim();

            bool emailExists = await _db.Users
                .AnyAsync(u =>
                    u.Email != null &&
                    u.Email.ToLower() == normalizedEmail);

            if (emailExists)
            {
                ViewBag.Error = "This email is already registered.";
                return View();
            }

            bool phoneExists = await _db.Users
                .AnyAsync(u =>
                    u.Phone != null &&
                    u.Phone == normalizedPhone);

            if (phoneExists)
            {
                ViewBag.Error = "This phone number is already registered.";
                return View();
            }

            var user = new User
            {
                FullName = fullName.Trim(),
                Email = normalizedEmail,
                Phone = normalizedPhone,
                Address = address.Trim(),
                UserType = "Adopter",
                ApprovalStatus = "Approved",
                PasswordHash = PasswordHelper.HashPassword(password)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Account created successfully. Please sign in.";
            return RedirectToAction("SignIn");
        }

        [HttpGet]
        public IActionResult AdminLogin()
        {
            return RedirectToAction("SignIn");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(string email, string password)
        {
            return await SignIn(email, password, false);
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("SignIn");
        }
    }
}