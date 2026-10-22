using Microsoft.AspNetCore.Mvc;

namespace Software_SARA.Controllers
{
    public class HomeController1 : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
