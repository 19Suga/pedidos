using Microsoft.AspNetCore.Mvc;

namespace OrderApp.Controllers
{
    public class OrdersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
