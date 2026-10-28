using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;
using PetWAMS.Web.Hubs;
using PetWAMS.Web.Models;

namespace PetWAMS.Web.Areas.Adopter.Controllers
{
    [Area("Adopter")]
    public class LostFoundController : Controller
    {
        private readonly PetWamsDbContext _db;
        private readonly IHubContext<NotificationHub> _hubContext;

        public LostFoundController(
            PetWamsDbContext db,
            IHubContext<NotificationHub> hubContext)
        {
            _db = db;
            _hubContext = hubContext;
        }


        // =========================
        // LOST & FOUND LIST
        // =========================
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null ||
                HttpContext.Session.GetString("UserType") != "Adopter")
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new { area = "" }
                );
            }

            ViewBag.CurrentUserID = userId.Value;

            ViewBag.LostPets = await _db.LostPets
                .Include(x => x.Pet)
                .OrderByDescending(x => x.LostID)
                .ToListAsync();

            ViewBag.FoundPets = await _db.FoundPets
                .Include(x => x.Pet)
                .OrderByDescending(x => x.FoundID)
                .ToListAsync();

            return View();
        }


        // =========================
        // CREATE LOST GET
        // =========================
        [HttpGet]
        public async Task<IActionResult> CreateLost()
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null ||
                HttpContext.Session.GetString("UserType") != "Adopter")
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new { area = "" }
                );
            }

            ViewBag.Pets = await _db.Adoptions
                .Where(a => a.UserID == userId.Value)
                .Include(a => a.Pet)
                .Select(a => a.Pet)
                .Where(p => p != null)
                .ToListAsync();

            return View();
        }


        // =========================
        // CREATE LOST POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLost(LostPet model)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null ||
                HttpContext.Session.GetString("UserType") != "Adopter")
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new { area = "" }
                );
            }

            bool ownsPet = await _db.Adoptions.AnyAsync(a =>
                a.UserID == userId.Value &&
                a.PetID == model.PetID);

            if (!ownsPet)
            {
                return BadRequest(
                    "You can only report your adopted pet as lost."
                );
            }

            model.UserID = userId.Value;

            if (ModelState.IsValid)
            {
                // SAVE LOST REPORT
                _db.LostPets.Add(model);

                await _db.SaveChangesAsync();


                // GET USER
                var user = await _db.Users
                    .FirstOrDefaultAsync(u =>
                        u.UserID == userId.Value);


                // GET PET
                var pet = await _db.Pets
                    .FirstOrDefaultAsync(p =>
                        p.PetID == model.PetID);


                string userName =
                    user?.FullName ?? "An adopter";

                string petName =
                    pet?.PetName ?? "a pet";


                // =========================
                // CREATE ADMIN NOTIFICATION
                // =========================
                var notification =
                    new Notification
                    {
                        Title =
                            "New Lost Pet Report",

                        Message =
                            $"{userName} reported {petName} as lost.",

                        Type =
                            "LostPetReport",

                        RelatedID =
                            model.LostID,

                        Link =
                            "/Admin/LostFound",

                        IsRead =
                            false,

                        CreatedAt =
                            DateTime.Now
                    };


                _db.Notifications.Add(
                    notification
                );

                await _db.SaveChangesAsync();


                // =========================
                // REAL-TIME SIGNALR PUSH
                // =========================
                await _hubContext
                    .Clients
                    .All
                    .SendAsync(
                        "ReceiveNotification"
                    );


                return RedirectToAction("Index");
            }


            ViewBag.Pets = await _db.Adoptions
                .Where(a => a.UserID == userId.Value)
                .Include(a => a.Pet)
                .Select(a => a.Pet)
                .Where(p => p != null)
                .ToListAsync();


            return View(model);
        }


        // =========================
        // EDIT LOST GET
        // =========================
        [HttpGet]
        public async Task<IActionResult> EditLost(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null ||
                HttpContext.Session.GetString("UserType") != "Adopter")
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new { area = "" }
                );
            }


            var report = await _db.LostPets
                .FirstOrDefaultAsync(x =>
                    x.LostID == id &&
                    x.UserID == userId.Value);


            if (report == null)
            {
                return NotFound();
            }


            ViewBag.Pets = await _db.Adoptions
                .Where(a => a.UserID == userId.Value)
                .Include(a => a.Pet)
                .Select(a => a.Pet)
                .Where(p => p != null)
                .ToListAsync();


            return View(report);
        }


        // =========================
        // EDIT LOST POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLost(LostPet model)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null ||
                HttpContext.Session.GetString("UserType") != "Adopter")
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new { area = "" }
                );
            }


            var report = await _db.LostPets
                .FirstOrDefaultAsync(x =>
                    x.LostID == model.LostID &&
                    x.UserID == userId.Value);


            if (report == null)
            {
                return NotFound();
            }


            bool ownsPet = await _db.Adoptions.AnyAsync(a =>
                a.UserID == userId.Value &&
                a.PetID == model.PetID);


            if (!ownsPet)
            {
                return BadRequest(
                    "You can only use your own adopted pet."
                );
            }


            report.PetID =
                model.PetID;

            report.LostLocation =
                model.LostLocation;

            report.LostDate =
                model.LostDate;

            report.ContactNumber =
                model.ContactNumber;


            await _db.SaveChangesAsync();


            return RedirectToAction("Index");
        }


        // =========================
        // DELETE LOST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLost(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null ||
                HttpContext.Session.GetString("UserType") != "Adopter")
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new { area = "" }
                );
            }


            var report = await _db.LostPets
                .FirstOrDefaultAsync(x =>
                    x.LostID == id &&
                    x.UserID == userId.Value);


            if (report == null)
            {
                return NotFound();
            }


            _db.LostPets.Remove(
                report
            );


            await _db.SaveChangesAsync();


            return RedirectToAction("Index");
        }


        // =========================
        // CREATE FOUND GET
        // =========================
        [HttpGet]
        public async Task<IActionResult> CreateFound()
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null ||
                HttpContext.Session.GetString("UserType") != "Adopter")
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new { area = "" }
                );
            }


            ViewBag.Pets = await _db.Pets
                .OrderBy(p => p.PetName)
                .ToListAsync();


            return View();
        }


        // =========================
        // CREATE FOUND POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFound(FoundPet model)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null ||
                HttpContext.Session.GetString("UserType") != "Adopter")
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new { area = "" }
                );
            }


            model.UserID =
                userId.Value;


            if (ModelState.IsValid)
            {
                // SAVE FOUND REPORT
                _db.FoundPets.Add(
                    model
                );


                await _db.SaveChangesAsync();


                // GET USER
                var user =
                    await _db.Users
                        .FirstOrDefaultAsync(
                            u =>
                                u.UserID ==
                                userId.Value
                        );


                // GET PET
                var pet =
                    await _db.Pets
                        .FirstOrDefaultAsync(
                            p =>
                                p.PetID ==
                                model.PetID
                        );


                string userName =
                    user?.FullName
                    ?? "An adopter";


                string petName =
                    pet?.PetName
                    ?? "a pet";


                // =========================
                // CREATE ADMIN NOTIFICATION
                // =========================
                var notification =
                    new Notification
                    {
                        Title =
                            "New Found Pet Report",

                        Message =
                            $"{userName} submitted a found report for {petName}.",

                        Type =
                            "FoundPetReport",

                        RelatedID =
                            model.FoundID,

                        Link =
                            "/Admin/LostFound",

                        IsRead =
                            false,

                        CreatedAt =
                            DateTime.Now
                    };


                _db.Notifications.Add(
                    notification
                );


                await _db.SaveChangesAsync();


                // =========================
                // REAL-TIME SIGNALR PUSH
                // =========================
                await _hubContext
                    .Clients
                    .All
                    .SendAsync(
                        "ReceiveNotification"
                    );


                return RedirectToAction("Index");
            }


            ViewBag.Pets = await _db.Pets
                .OrderBy(p => p.PetName)
                .ToListAsync();


            return View(model);
        }


        // =========================
        // EDIT FOUND GET
        // =========================
        [HttpGet]
        public async Task<IActionResult> EditFound(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null ||
                HttpContext.Session.GetString("UserType") != "Adopter")
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new { area = "" }
                );
            }


            var report =
                await _db.FoundPets
                    .FirstOrDefaultAsync(
                        x =>
                            x.FoundID == id &&
                            x.UserID ==
                            userId.Value
                    );


            if (report == null)
            {
                return NotFound();
            }


            ViewBag.Pets =
                await _db.Pets
                    .OrderBy(
                        p =>
                            p.PetName
                    )
                    .ToListAsync();


            return View(report);
        }


        // =========================
        // EDIT FOUND POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFound(FoundPet model)
        {
            var userId =
                HttpContext.Session.GetInt32(
                    "UserID"
                );


            if (userId == null ||
                HttpContext.Session.GetString("UserType") != "Adopter")
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new { area = "" }
                );
            }


            var report =
                await _db.FoundPets
                    .FirstOrDefaultAsync(
                        x =>
                            x.FoundID ==
                            model.FoundID
                            &&
                            x.UserID ==
                            userId.Value
                    );


            if (report == null)
            {
                return NotFound();
            }


            report.PetID =
                model.PetID;

            report.FoundLocation =
                model.FoundLocation;

            report.FoundDate =
                model.FoundDate;

            report.ReporterName =
                model.ReporterName;

            report.ContactNumber =
                model.ContactNumber;


            await _db.SaveChangesAsync();


            return RedirectToAction("Index");
        }


        // =========================
        // DELETE FOUND
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFound(int id)
        {
            var userId =
                HttpContext.Session.GetInt32(
                    "UserID"
                );


            if (userId == null ||
                HttpContext.Session.GetString("UserType") != "Adopter")
            {
                return RedirectToAction(
                    "SignIn",
                    "Account",
                    new { area = "" }
                );
            }


            var report =
                await _db.FoundPets
                    .FirstOrDefaultAsync(
                        x =>
                            x.FoundID == id
                            &&
                            x.UserID ==
                            userId.Value
                    );


            if (report == null)
            {
                return NotFound();
            }


            _db.FoundPets.Remove(
                report
            );


            await _db.SaveChangesAsync();


            return RedirectToAction("Index");
        }
    }
}