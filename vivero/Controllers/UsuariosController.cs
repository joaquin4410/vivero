using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Threading.Tasks;
using vivero.Models;
using System.Net.Mail;
using System.Net;

namespace vivero.Controllers
{
    [Authorize(Roles = "Administrador,Trabajador")]
    public class UsuariosController : Controller
    {
        private readonly AppDbContext _context;

        public UsuariosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Usuarios
        public async Task<IActionResult> Index()
        {
            return _context.Usuarios != null ?
                        View(await _context.Usuarios.ToListAsync()) :
                        Problem("Entity set 'AppDbContext.Usuarios' is null.");
        }

        // GET: Usuarios/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null || _context.Usuarios == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(m => m.Rut == id);
            if (usuario == null)
            {
                return NotFound();
            }

            return View(usuario);
        }
        private async Task RegistrarActividad(string usuarioId, string accion, string detalles)
        {
            var actividad = new HistorialActividad
            {
                UsuarioId = usuarioId,
                Accion = accion,
                FechaHora = DateTime.Now,
                Detalles = detalles
            };

            _context.HistorialActividades.Add(actividad);
            await _context.SaveChangesAsync();
        }


        // GET: Usuarios/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Rut,Nombre,Email,Rol,isBlock")] Usuario usuario, string password)
        {
            // Verificar si el correo ya está en uso
            if (_context.Usuarios.Any(u => u.Email == usuario.Email))
            {
                ModelState.AddModelError("Email", "El correo electrónico ya está en uso.");
                return View(usuario);
            }

            if (ModelState.IsValid)
            {
                // Validar que se haya ingresado una contraseña
                if (string.IsNullOrWhiteSpace(password))
                {
                    ModelState.AddModelError("Password", "Debe ingresar una contraseña.");
                    return View(usuario);
                }

                // Generar el hash y salt de la contraseña
                CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

                usuario.PasswordHash = passwordHash;
                usuario.PasswordSalt = passwordSalt;

                // Guardar el usuario en la base de datos
                _context.Add(usuario);
                await _context.SaveChangesAsync();

                // Registrar actividad (opcional)
                await RegistrarActividad(User.Identity.Name, "Crear Usuario", $"Usuario creado: {usuario.Nombre}");
                await new AuthController(_context).EnviarCorreo(usuario.Email, "Registro exitoso", $"Hola {usuario.Nombre}, te has registrado exitosamente en nuestro sistema.");

                return RedirectToAction(nameof(Index));

            }
            return View(usuario);
        }

        // GET: Usuarios/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null || _context.Usuarios == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            return View(usuario);
        }

        // POST: Usuarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Rut,Nombre,Email,Rol,isBlock")] Usuario usuario)
        {
            if (id != usuario.Rut)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Buscar el usuario existente en la base de datos
                    var usuarioExistente = await _context.Usuarios.FindAsync(id);
                    if (usuarioExistente == null)
                    {
                        return NotFound();
                    }

                    // Actualizar solo los campos permitidos
                    usuarioExistente.Nombre = usuario.Nombre;
                    usuarioExistente.Email = usuario.Email;
                    usuarioExistente.Rol = usuario.Rol;
                    usuarioExistente.isBlock = usuario.isBlock;

                    // Marcar explícitamente la entidad como modificada
                    _context.Entry(usuarioExistente).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsuarioExists(usuario.Rut))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(usuario);
        }

        // GET: Usuarios/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null || _context.Usuarios == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(m => m.Rut == id);
            if (usuario == null)
            {
                return NotFound();
            }

            return View(usuario);
        }

        // POST: Usuarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id)
        {
            var usuario = _context.Usuarios.Find(id);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool UsuarioExists(string id)
        {
            return (_context.Usuarios?.Any(e => e.Rut == id)).GetValueOrDefault();
        }

        // Método para generar hash de contraseña
        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }
    }
}
