using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderApp.Data;
using OrderApp.Models;
using System.Security.Claims;
using System.Text.Json;

namespace OrderApp.Controllers
{
    [Authorize] // acceso sólo autenticados a todo el controlador
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private const string CART_KEY = "CART_ITEMS";

        public OrdersController(ApplicationDbContext db) => _db = db;

        // ===== Helpers de carrito / usuario =====
        private List<CartItem> GetCart()
        {
            var json = HttpContext.Session.GetString(CART_KEY);
            return string.IsNullOrEmpty(json)
                ? new List<CartItem>()
                : (JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>());
        }

        private void SaveCart(List<CartItem> items) =>
            HttpContext.Session.SetString(CART_KEY, JsonSerializer.Serialize(items));

        private int GetCurrentUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ===== Carrito =====
        public IActionResult Cart()
        {
            var cart = GetCart();
            ViewBag.Subtotal = cart.Sum(i => i.UnitPrice * i.Quantity);
            ViewBag.Tax = 0m; // ajusta si aplicas impuestos
            ViewBag.Total = (decimal)ViewBag.Subtotal + (decimal)ViewBag.Tax;
            return View(cart);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AddToCart(int productId, int qty = 1)
        {
            var product = _db.Products.Find(productId);
            if (product == null)
            {
                TempData["Error"] = "Producto no encontrado.";
                return RedirectToAction("Index", "Products");
            }

            if (qty < 1) qty = 1;

            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.ProductId == productId);
            if (item == null)
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    UnitPrice = product.Price,
                    Quantity = qty
                });
            }
            else
            {
                item.Quantity += qty;
            }

            SaveCart(cart);
            TempData["Success"] = "Producto agregado al carrito.";
            return RedirectToAction("Cart");
        }

        // ===== Checkout / creación de pedido =====
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Checkout()
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["Error"] = "El carrito está vacío.";
                return RedirectToAction("Cart");
            }

            // Validar stock antes de crear pedido
            foreach (var ci in cart)
            {
                var p = _db.Products.Find(ci.ProductId);
                if (p == null || p.Stock < ci.Quantity)
                {
                    TempData["Error"] = $"Stock insuficiente de {ci?.Name ?? "producto"}.";
                    return RedirectToAction("Cart");
                }
            }

            try
            {
                var order = new Order
                {
                    UserId = GetCurrentUserId(),
                    Status = "Pending",
                    Subtotal = cart.Sum(i => i.UnitPrice * i.Quantity),
                    Tax = 0m
                };
                order.Total = order.Subtotal + order.Tax;

                _db.Orders.Add(order);
                _db.SaveChanges();

                // Crear items y ajustar stock
                foreach (var ci in cart)
                {
                    var p = _db.Products.Find(ci.ProductId)!;

                    _db.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = p.Id,
                        Quantity = ci.Quantity,
                        UnitPrice = p.Price // copia del precio en el momento del pedido
                    });

                    p.Stock -= ci.Quantity;
                }

                _db.SaveChanges();

                // Limpiar carrito
                SaveCart(new List<CartItem>());
                TempData["Success"] = "Pedido creado correctamente.";
                return RedirectToAction("Details", new { id = order.Id });
            }
            catch
            {
                TempData["Error"] = "Ocurrió un error al crear el pedido. Intenta de nuevo.";
                return RedirectToAction("Cart");
            }
        }

        // ===== Listado / detalle =====
        public async Task<IActionResult> Index()
        {
            var isAdminOrEmployee = User.IsInRole("Admin") || User.IsInRole("Empleado");
            var uid = GetCurrentUserId();

            var orders = await _db.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            if (!isAdminOrEmployee)
                orders = orders.Where(o => o.UserId == uid).ToList();

            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // El dueño puede ver su pedido; Admin/Empleado pueden ver todos
            var uid = GetCurrentUserId();
            if (order.UserId != uid && !(User.IsInRole("Admin") || User.IsInRole("Empleado")))
                return Forbid();

            return View(order);
        }

        // ===== Cambiar estado (PASO 3) =====
        [Authorize(Roles = "Admin,Empleado")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, string status)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();

            var allowed = new[] { "Pending", "Processed", "Shipped", "Delivered" };
            if (!allowed.Contains(status)) status = "Pending";

            order.Status = status;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Estado del pedido actualizado.";
            return RedirectToAction("Details", new { id });
        }
    }
}
