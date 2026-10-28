using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Data;

namespace PetWAMS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PaymentController : Controller
    {
        private readonly PetWamsDbContext _db;

        public PaymentController(PetWamsDbContext db)
        {
            _db = db;
        }


        public async Task<IActionResult> Index()
        {
            var payments = await _db.PaymentTransactions
                .Include(p => p.User)
                .Include(p => p.Order)
                .OrderByDescending(p => p.TransactionDate)
                .ToListAsync();


            return View(payments);
        }
    }
}