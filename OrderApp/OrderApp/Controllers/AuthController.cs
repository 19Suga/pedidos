using Microsoft.AspNetCore.Mvc;

namespace OrderApp.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
