using Microsoft.AspNetCore.Mvc;

namespace PetWAMS.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
