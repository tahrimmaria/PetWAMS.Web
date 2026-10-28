using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;

namespace PetWAMS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : Controller
    {
        private readonly PetWamsDbContext _db;

        public DashboardController(PetWamsDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UserType") != "Admin")
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }

            ViewBag.FullName = HttpContext.Session.GetString("FullName") ?? "Admin User";

            ViewBag.TotalUsers = await _db.Users.CountAsync();
            ViewBag.TotalShelters = await _db.Shelters.CountAsync();
            ViewBag.TotalPets = await _db.Pets.CountAsync();
            ViewBag.TotalRequests = await _db.AdoptionRequests.CountAsync();

            ViewBag.UnreadNotificationsCount = await _db.Notifications
                .CountAsync(n => n.IsRead == false);

            ViewBag.RecentNotifications = await _db.Notifications
                .Where(n => n.IsRead == false)
                .OrderByDescending(n => n.CreatedAt)
                .Take(8)
                .ToListAsync();

            return View();
        }

        public async Task<IActionResult> OpenNotification(int id)
        {
            if (HttpContext.Session.GetString("UserType") != "Admin")
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }

            var notification = await _db.Notifications
                .FirstOrDefaultAsync(n => n.NotificationID == id);

            if (notification == null)
            {
                return RedirectToAction("Index");
            }

            notification.IsRead = true;
            await _db.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(notification.Link) &&
                Url.IsLocalUrl(notification.Link))
            {
                return Redirect(notification.Link);
            }

            return RedirectToAction("Index", "AdoptionRequests", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            if (HttpContext.Session.GetString("UserType") != "Admin")
            {
                return RedirectToAction("SignIn", "Account", new { area = "" });
            }

            var unreadNotifications = await _db.Notifications
                .Where(n => n.IsRead == false)
                .ToListAsync();

            if (unreadNotifications.Count > 0)
            {
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                }

                await _db.SaveChangesAsync();
            }

            TempData["Success"] = "All notifications marked as read.";
            return RedirectToAction("Index");
        }
    }
}