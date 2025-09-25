using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderApp.Data;
using OrderApp.Models;

namespace OrderApp.Controllers
{
    [Authorize(Roles = "Admin")] // solo Admin puede gestionar usuarios
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        public UsersController(ApplicationDbContext context) => _context = context;

        // GET: Users
        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.OrderBy(u => u.Name).ToListAsync());
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var user = await _context.Users.FirstOrDefaultAsync(m => m.Id == id);
            if (user == null) return NotFound();
            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create() => View();

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Email,Password,Role")] User form)
        {
            if (!ModelState.IsValid) return View(form);

            // Normalizar rol: Admin / Empleado / Cliente
            form.Role = (form.Role ?? "Cliente").Trim();
            if (form.Role.Equals("admin", StringComparison.OrdinalIgnoreCase)) form.Role = "Admin";
            else if (form.Role.Equals("empleado", StringComparison.OrdinalIgnoreCase)) form.Role = "Empleado";
            else form.Role = "Cliente";

            // Hash de contraseña
            form.Password = SimpleHasher.Hash(form.Password);

            _context.Add(form);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Usuario creado.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,Password,Role")] User form)
        {
            if (id != form.Id) return NotFound();
            if (!ModelState.IsValid) return View(form);

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.Name = form.Name;
            user.Email = form.Email;

            // Normalizar rol
            var role = (form.Role ?? "Cliente").Trim();
            if (role.Equals("admin", StringComparison.OrdinalIgnoreCase)) user.Role = "Admin";
            else if (role.Equals("empleado", StringComparison.OrdinalIgnoreCase)) user.Role = "Empleado";
            else user.Role = "Cliente";

            // Si vino password nueva (no vacía), re-hashear
            if (!string.IsNullOrWhiteSpace(form.Password))
                user.Password = SimpleHasher.Hash(form.Password);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Usuario actualizado.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var user = await _context.Users.FirstOrDefaultAsync(m => m.Id == id);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null) _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Usuario eliminado.";
            return RedirectToAction(nameof(Index));
        }
    }
}
