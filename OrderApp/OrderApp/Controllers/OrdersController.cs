using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderApp.Data;
using OrderApp.Models;
using System.Security.Claims;
using System.Text.Json;

namespace OrderApp.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private const string CART_KEY = "CART_ITEMS";

        public OrdersController(ApplicationDbContext db) => _db = db;

        private List<CartItem> GetCart()
        {
            var json = HttpContext.Session.GetString(CART_KEY);
            return string.IsNullOrEmpty(json) ? new List<CartItem>() : (JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>());
        }
        private void SaveCart(List<CartItem> items) => HttpContext.Session.SetString(CART_KEY, JsonSerializer.Serialize(items));
        private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public IActionResult Cart()
        {
            var cart = GetCart();
            ViewBag.Subtotal = cart.Sum(i => i.UnitPrice * i.Quantity);
            ViewBag.Tax = 0m;
            ViewBag.Total = (decimal)ViewBag.Subtotal;
            return View(cart);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AddToCart(int productId, int qty = 1)
        {
            var product = _db.Products.Find(productId);
            if (product == null) return RedirectToAction("Index", "Products");

            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.ProductId == productId);
            if (item == null)
                cart.Add(new CartItem { ProductId = product.Id, Name = product.Name, UnitPrice = product.Price, Quantity = qty });
            else
                item.Quantity += qty;

            SaveCart(cart);
            return RedirectToAction("Cart");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Checkout()
        {
            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Cart");

            // validar stock
            foreach (var ci in cart)
            {
                var p = _db.Products.Find(ci.ProductId);
                if (p == null || p.Stock < ci.Quantity)
                {
                    TempData["Error"] = $"Stock insuficiente de {ci?.Name}";
                    return RedirectToAction("Cart");
                }
            }

            var order = new Order
            {
                UserId = GetCurrentUserId(),
                Status = "Pending",
                Subtotal = cart.Sum(i => i.UnitPrice * i.Quantity),
                Tax = 0m,
            };
            order.Total = order.Subtotal;

            _db.Orders.Add(order);
            _db.SaveChanges();

            foreach (var ci in cart)
            {
                var p = _db.Products.Find(ci.ProductId)!;
                _db.OrderItems.Add(new OrderItem { OrderId = order.Id, ProductId = p.Id, Quantity = ci.Quantity, UnitPrice = p.Price });
                p.Stock -= ci.Quantity;
            }
            _db.SaveChanges();

            SaveCart(new List<CartItem>());
            return RedirectToAction("Details", new { id = order.Id });
        }

        public async Task<IActionResult> Index()
        {
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("Empleado");
            var uid = GetCurrentUserId();

            var orders = await _db.Orders.Include(o => o.User).OrderByDescending(o => o.Id).ToListAsync();
            if (!isAdmin) orders = orders.Where(o => o.UserId == uid).ToList();

            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _db.Orders.Include(o => o.User).Include(o => o.Items).ThenInclude(i => i.Product).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            return View(order);
        }
    }
}
