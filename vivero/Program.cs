using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using vivero.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login"; // Ruta de inicio de sesión
        options.LogoutPath = "/Auth/LogOut"; // Ruta de cierre de sesión
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.Cookie.HttpOnly = true; // Mejora la seguridad de la cookie
        options.Cookie.SameSite = SameSiteMode.Lax; // Ayuda a prevenir ataques CSRF
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Asegura la cookie en entornos seguros
    });

// Registrar servicios adicionales
builder.Services.AddSession(); // Habilitar sesiones
builder.Services.AddMemoryCache(); // Habilitar cache en memoria
builder.Services.AddHttpContextAccessor(); // Acceso a HttpContext en otras clases

// Registrar HttpClient
builder.Services.AddHttpClient(); // Configuración necesaria para HttpClient

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // Manejo de excepciones
    app.UseHsts(); // Seguridad adicional
}

app.UseHttpsRedirection(); // Redirección a HTTPS
app.UseStaticFiles(); // Habilitar archivos estáticos

app.UseRouting(); // Habilitar el enrutamiento
app.UseAuthentication(); // Habilitar autenticación
app.UseAuthorization(); // Habilitar autorización
app.UseSession(); // Habilitar sesiones

app.UseResponseCaching();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"); // Ruta por defecto

app.Run(); // Ejecutar la aplicación
