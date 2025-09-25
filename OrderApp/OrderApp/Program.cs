using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using OrderApp.Data;
using OrderApp.Models;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Authentication (Cookies)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Authorization
builder.Services.AddAuthorization();

// Session
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.IdleTimeout = TimeSpan.FromHours(2);
});

var app = builder.Build();

// Seed admin (con try/catch opcional para evitar caída con errores de conexión/migración)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        db.Database.Migrate();

        if (!db.Users.Any(u => u.Email == "admin@local"))
        {
            var admin = new User
            {
                Name = "Admin",
                Email = "admin@local",
                Role = "Admin",
                Password = SimpleHasher.Hash("Admin123!")
            };
            db.Users.Add(admin);
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error al migrar/sembrar la base de datos: " + ex.Message);
        throw; // si prefieres que no se caiga, comenta esta línea
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Helper hash sencillo (solo demo) — ahora tolera null/empty
public static class SimpleHasher
{
    public static string Hash(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
