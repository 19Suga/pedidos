using Microsoft.AspNetCore.Mvc;

namespace OrderApp.Controllers
{
    public class UsersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
