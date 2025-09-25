using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OrderApp.Data;
using OrderApp.Models;

namespace OrderApp.Controllers
{
    [Authorize] // cualquier usuario autenticado (Admin, Empleado, Cliente)
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ProductsController(ApplicationDbContext context) => _context = context;

        // GET: Products (todos los roles autenticados)
        public async Task<IActionResult> Index(string? searchString, string? categoryFilter, decimal? minPrice, decimal? maxPrice)
        {
            var products = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
                products = products.Where(p => p.Name.Contains(searchString) || p.Description.Contains(searchString));

            if (!string.IsNullOrWhiteSpace(categoryFilter))
                products = products.Where(p => p.Category == categoryFilter);

            if (minPrice.HasValue)
                products = products.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                products = products.Where(p => p.Price <= maxPrice.Value);

            var categories = await _context.Products
                .Where(p => !string.IsNullOrEmpty(p.Category))
                .Select(p => p.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.Categories = new SelectList(categories);

            var list = await products.OrderBy(p => p.Name).ToListAsync();
            return View(list);
        }

        // GET: Products/Details/5 (todos los roles autenticados)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        // ===== Acciones SOLO Admin/Empleado =====
        [Authorize(Roles = "Admin,Empleado")]
        public IActionResult Create() => View();

        [Authorize(Roles = "Admin,Empleado")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Price,Stock,Category,IsActive")] Product product)
        {
            if (!ModelState.IsValid) return View(product);

            product.CreatedAt = DateTime.Now;
            product.UpdatedAt = DateTime.Now;

            _context.Add(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Empleado")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [Authorize(Roles = "Admin,Empleado")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,Stock,Category,IsActive,CreatedAt")] Product product)
        {
            if (id != product.Id) return NotFound();
            if (!ModelState.IsValid) return View(product);

            try
            {
                product.UpdatedAt = DateTime.Now;
                _context.Update(product);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Products.Any(e => e.Id == product.Id))
                    return NotFound();
                else
                    throw;
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Empleado")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        [Authorize(Roles = "Admin,Empleado")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null) _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
