using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderApp.Data;
using OrderApp.Models;
using System.Security.Claims;

namespace OrderApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _db;
        public AuthController(ApplicationDbContext db) => _db = db;

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            var hashed = SimpleHasher.Hash(password);
            var user = _db.Users.FirstOrDefault(u => u.Email == email && u.Password == hashed);
            if (user == null)
            {
                TempData["Error"] = "Credenciales inválidas.";
                return View();
            }

            // Normalizar rol guardado en BD a: Admin / Empleado / Cliente
            var roleRaw = (user.Role ?? "Cliente").Trim();
            string roleNormalized;
            if (roleRaw.Equals("admin", StringComparison.OrdinalIgnoreCase)) roleNormalized = "Admin";
            else if (roleRaw.Equals("empleado", StringComparison.OrdinalIgnoreCase)) roleNormalized = "Empleado";
            else roleNormalized = "Cliente";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, roleNormalized)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}
