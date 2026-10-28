using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Helpers;
using PetWAMS.Web.Models;
using System.Text;

namespace PetWAMS.Web.Controllers
{
    public class PasswordFixController : Controller
    {
        private readonly PetWamsDbContext _db;

        public PasswordFixController(PetWamsDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> ShelterStaff()
        {
            string defaultPassword = "staff123";
            string passwordHash = PasswordHelper.HashPassword(defaultPassword);

            var shelters = await _db.Shelters
                .OrderBy(s => s.ShelterID)
                .ToListAsync();

            if (shelters.Count == 0)
            {
                return Content("No shelter found in database.", "text/plain");
            }

            var existingEmails = await _db.Users
                .Select(u => u.Email.ToLower())
                .ToListAsync();

            var existingPhones = await _db.Users
                .Select(u => u.Phone)
                .ToListAsync();

            var result = new StringBuilder();

            result.AppendLine("Shelter staff setup successful.");
            result.AppendLine("Password for all shelter staff: staff123");
            result.AppendLine("---------------------------------------");

            foreach (var shelter in shelters)
            {
                var staff = await _db.Users
                    .FirstOrDefaultAsync(u =>
                        u.ShelterID == shelter.ShelterID &&
                        (
                            u.UserType == "Staff" ||
                            u.UserType == "Shelter"
                        ));

                if (staff == null)
                {
                    string email = "shelter" + shelter.ShelterID + "@pethope.com";
                    int emailCounter = 1;

                    while (existingEmails.Contains(email.ToLower()))
                    {
                        email = "shelter" + shelter.ShelterID + "_" + emailCounter + "@pethope.com";
                        emailCounter++;
                    }

                    existingEmails.Add(email.ToLower());

                    string phone = "01800" + shelter.ShelterID.ToString().PadLeft(6, '0');
                    int phoneCounter = 1;

                    while (existingPhones.Contains(phone))
                    {
                        phone = "01801" + shelter.ShelterID.ToString().PadLeft(5, '0') + phoneCounter;
                        phoneCounter++;
                    }

                    existingPhones.Add(phone);

                    staff = new User
                    {
                        FullName = shelter.ShelterName + " Staff",
                        Email = email,
                        Phone = phone,
                        Address = string.IsNullOrWhiteSpace(shelter.Address)
                            ? "Not provided"
                            : shelter.Address,
                        UserType = "Staff",
                        ApprovalStatus = "Approved",
                        ShelterID = shelter.ShelterID,
                        PasswordHash = passwordHash
                    };

                    _db.Users.Add(staff);
                }
                else
                {
                    staff.UserType = "Staff";
                    staff.ApprovalStatus = "Approved";
                    staff.ShelterID = shelter.ShelterID;
                    staff.PasswordHash = passwordHash;
                }

                result.AppendLine(
                    "ShelterID: " + shelter.ShelterID +
                    " | Shelter: " + shelter.ShelterName +
                    " | Staff Email: " + staff.Email +
                    " | Password: " + defaultPassword
                );
            }

            await _db.SaveChangesAsync();

            return Content(result.ToString(), "text/plain");
        }

        public async Task<IActionResult> Tamim()
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == "tamim@gmail.com");

            if (user == null)
            {
                return Content("Tamim user not found.", "text/plain");
            }

            user.PasswordHash = PasswordHelper.HashPassword("staff123");
            user.UserType = "Staff";
            user.ApprovalStatus = "Approved";
            user.ShelterID = 4;

            await _db.SaveChangesAsync();

            return Content("Tamim fixed. Email: tamim@gmail.com | Password: staff123 | ShelterID: 4", "text/plain");
        }
    }
}