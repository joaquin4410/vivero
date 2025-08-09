using vivero.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace vivero.Controllers
{

    public class AuthController : Controller
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        // GET: AuthController/Login
        public IActionResult Login(int? productoId, int? redirectProductos)
        {
            // Verificar si ya está autenticado
            if (User.Identity.IsAuthenticated)
            {
                if (redirectProductos.HasValue && redirectProductos.Value == 1)
                {
                    return RedirectToAction("ProductosDisponibles", "Productos");
                }
                return RedirectToAction("Index", "Dashboard");
            }
            ViewBag.ProductoId = productoId; // Almacenar el id del producto en ViewBag
            ViewBag.RedirectProductos = redirectProductos;
            return View();
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            // Redirigir siempre a ProductosDisponibles
            ViewBag.ReturnUrl = Url.Action("ProductosDisponibles", "Productos");
            return View();
        }



        public IActionResult CreateWorker()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWorker(string Rut, string Nombre, string Email, string Password, string Rol)
        {
            // Validación de RUT
            if (string.IsNullOrWhiteSpace(Rut) || !System.Text.RegularExpressions.Regex.IsMatch(Rut, @"^\d{7,8}-[0-9kK]$"))
            {
                ModelState.AddModelError("Rut", "El RUT debe tener entre 8 y 9 caracteres con guion. Ejemplo: 12345678-9");
                return View();
            }

            if (_context.Usuarios.Any(u => u.Rut == Rut))
            {
                ModelState.AddModelError("Rut", "El RUT ya está en uso.");
                return View();
            }

            // Validación de Nombre
            if (string.IsNullOrWhiteSpace(Nombre) || !System.Text.RegularExpressions.Regex.IsMatch(Nombre, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ]+( [a-zA-ZáéíóúÁÉÍÓÚñÑ]+)+$"))
            {
                ModelState.AddModelError("Nombre", "Debe ingresar un nombre completo válido, sin números.");
                return View();
            }

            // Validación de Email
            if (_context.Usuarios.Any(u => u.Email == Email))
            {
                ModelState.AddModelError("Email", "El correo electrónico ya está en uso.");
                return View();
            }

            // Validación de Contraseña
            if (string.IsNullOrEmpty(Password) || Password.Length < 8)
            {
                ModelState.AddModelError("Password", "La contraseña debe tener al menos 8 caracteres.");
                ViewBag.PasswordStrength = "Débil";
                return View();
            }

            // Evaluar fuerza de la contraseña
            var strength = EvaluatePasswordStrength(Password);
            ViewBag.PasswordStrength = strength;
            if (strength == "Débil")
            {
                ModelState.AddModelError("Password", "La contraseña es demasiado débil.");
                return View();
            }

            // Validación de Rol
            if (Rol != "Trabajador" && Rol != "Administrador")
            {
                ModelState.AddModelError("Rol", "El rol seleccionado no es válido.");
                return View();
            }

            // Crear el usuario
            var nuevoUsuario = new Usuario
            {
                Rut = Rut,
                Nombre = Nombre,
                Email = Email,
                Rol = Rol,
                isBlock = false
            };

            // Generar el hash y la sal de la contraseña
            CreatePasswordHash(Password, out byte[] passwordHash, out byte[] passwordSalt);
            nuevoUsuario.PasswordHash = passwordHash;
            nuevoUsuario.PasswordSalt = passwordSalt;

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Usuarios");
        }

        private string EvaluatePasswordStrength(string password)
        {
            int score = 0;

            if (password.Length >= 8) score++;
            if (password.Any(char.IsUpper)) score++;
            if (password.Any(char.IsLower)) score++;
            if (password.Any(char.IsDigit)) score++;
            if (password.Any(ch => !char.IsLetterOrDigit(ch))) score++;

            return score switch
            {
                5 => "Muy Fuerte",
                4 => "Fuerte",
                3 => "Media",
                _ => "Débil",
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string EmailForm, string pass, int? productoId, int? redirectProductos)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(x => x.Email == EmailForm);

            if (usuario != null)
            {
                if (VerificarPass(pass, usuario.PasswordHash, usuario.PasswordSalt))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, usuario.Rut),
                        new Claim(ClaimTypes.Role, usuario.Rol)
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                        new AuthenticationProperties { IsPersistent = true });

                    if (redirectProductos.HasValue && redirectProductos.Value == 1)
                    {
                        return RedirectToAction("ProductosDisponibles", "Productos");
                    }

                    return RedirectToAction("Index", "Dashboard");
                }
                else
                {
                    ModelState.AddModelError("", "Contraseña incorrecta");
                }
            }
            else
            {
                ModelState.AddModelError("", "Usuario no encontrado");
            }

            ViewBag.RedirectProductos = redirectProductos;
            return View();
        }

        public IActionResult CreateAdmin()
        {
            try
            {
                // Crear un usuario administrador si no existe
                if (!_context.Usuarios.Any(u => u.Rut == "admin"))
                {
                    var U = new Usuario
                    {
                        Nombre = "Admin",
                        Email = "admin@admin.cl",
                        Rut = "admin",
                        Rol = "Administrador",
                        isBlock = false
                    };

                    CreatePasswordHash("admin", out byte[] passwordHash, out byte[] passwordSalt);

                    U.PasswordHash = passwordHash;
                    U.PasswordSalt = passwordSalt;
                    _context.Usuarios.Add(U);
                    _context.SaveChanges();
                }
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("Auth/RegisterClient")]
        public IActionResult RegisterClient()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [Route("Auth/RegisterClient")]
        public async Task<IActionResult> RegisterClient(string Rut, string Nombre, string Email, string Password)
        {
            if (_context.Usuarios.Any(u => u.Email == Email))
            {
                ModelState.AddModelError("Email", "El correo electrónico ya está en uso.");
                return View();
            }

            if (string.IsNullOrWhiteSpace(Rut) || _context.Usuarios.Any(u => u.Rut == Rut))
            {
                ModelState.AddModelError("Rut", "El Rut es obligatorio y debe ser único.");
                return View();
            }

            if (Password.Length < 8 || !Password.Any(char.IsUpper))
            {
                ModelState.AddModelError("Password", "La contraseña debe tener al menos 8 caracteres y una letra mayúscula.");
                return View();
            }

            CreatePasswordHash(Password, out byte[] passwordHash, out byte[] passwordSalt);

            var cliente = new Usuario
            {
                Rut = Rut,
                Nombre = Nombre,
                Email = Email,
                Rol = "Cliente",
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt
            };

            _context.Usuarios.Add(cliente);
            await _context.SaveChangesAsync();
            await EnviarCorreo(cliente.Email, "Registro exitoso", $"Hola {cliente.Nombre}, te has registrado exitosamente en nuestro sistema.");
            TempData["Mensaje"] = "Cuenta creada exitosamente. Ahora puedes iniciar sesión.";
            return RedirectToAction("Login", "Auth");
        }



        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }

        public void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        public bool VerificarPass(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512(passwordSalt))
            {
                var pass = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return pass.SequenceEqual(passwordHash);
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string Email)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == Email);
            if (usuario == null)
            {
                ModelState.AddModelError("", "No se encontró una cuenta asociada a este correo.");
                return View();
            }

            // Generar un token único
            var token = Guid.NewGuid().ToString();

            // Crear una solicitud de restablecimiento de contraseña
            var passwordResetRequest = new PasswordResetRequest
            {
                Email = Email,
                Token = token,
                RequestDate = DateTime.UtcNow
            };

            _context.PasswordResetRequests.Add(passwordResetRequest);
            await _context.SaveChangesAsync();

            // Enviar correo con el enlace para restablecer la contraseña
            var resetLink = Url.Action("ResetPassword", "Auth", new { token }, Request.Scheme);
            await EnviarCorreo(Email, "Restablecimiento de Contraseña",
                $"Haz clic en el siguiente enlace para restablecer tu contraseña: {resetLink}");

            TempData["Mensaje"] = "Se ha enviado un correo con las instrucciones para recuperar tu contraseña.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            var model = new ResetPasswordViewModel { Token = token };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var resetRequest = await _context.PasswordResetRequests
                .FirstOrDefaultAsync(r => r.Token == model.Token && !r.IsUsed);

            if (resetRequest == null || resetRequest.RequestDate.AddHours(24) < DateTime.UtcNow)
            {
                ModelState.AddModelError("", "El enlace de restablecimiento es inválido o ha expirado.");
                return View(model);
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == resetRequest.Email);
            if (usuario == null)
            {
                ModelState.AddModelError("", "El usuario asociado no se encontró.");
                return View(model);
            }

            // Actualizar la contraseña
            CreatePasswordHash(model.Password, out byte[] passwordHash, out byte[] passwordSalt);
            usuario.PasswordHash = passwordHash;
            usuario.PasswordSalt = passwordSalt;

            // Marcar la solicitud como utilizada
            resetRequest.IsUsed = true;

            _context.Update(usuario);
            _context.Update(resetRequest);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Tu contraseña ha sido restablecida exitosamente.";
            return RedirectToAction("Login");
        }

        public async Task EnviarCorreo(string destinatario, string asunto, string mensaje)
        {
            using (var smtp = new SmtpClient())
            {
                smtp.Host = "smtp.gmail.com";
                smtp.Port = 587;
                smtp.EnableSsl = true;
                smtp.Credentials = new NetworkCredential("ncjvivero@gmail.com", "ivrzgkeietuvqbcu");

                var correo = new MailMessage
                {
                    From = new MailAddress("ncjvivero@gmail.com", "Vivero NCJ"),
                    Subject = asunto,
                    Body = mensaje,
                    IsBodyHtml = true
                };

                correo.To.Add(destinatario);
                await smtp.SendMailAsync(correo);
            }
        }
    }
}
