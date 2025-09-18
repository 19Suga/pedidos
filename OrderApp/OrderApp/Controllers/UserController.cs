using Microsoft.AspNetCore.Mvc;

namespace OrderApp.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
