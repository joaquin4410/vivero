using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using vivero.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace vivero.Controllers
{
    [Authorize(Roles = "Administrador,Trabajador")]
    public class ProveedoresController : Controller
    {
        private readonly AppDbContext _context;

        public ProveedoresController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public IActionResult Index()
        {
            var proveedores = _context.Proveedores.ToList();
            return View(proveedores);
        }

        // GET: Proveedores/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Proveedores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Proveedor proveedor)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(proveedor);
                    await _context.SaveChangesAsync();

                    // Registrar actividad
                    await RegistrarActividad(User.Identity.Name, "Crear Proveedor", $"Proveedor creado: {proveedor.Nombre}");

                    return RedirectToAction(nameof(Index));
                }
                catch
                {
                    ModelState.AddModelError("", "Error al guardar el proveedor.");
                }
            }

            // Si el modelo no es válido, la vista se muestra con los errores
            return View(proveedor);
        }


        // Método para registrar actividades
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


        // GET: Proveedores/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null)
            {
                return NotFound();
            }
            return View(proveedor);
        }

        // POST: Proveedores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Proveedor proveedor)
        {
            if (id != proveedor.ProveedorId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(proveedor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProveedorExists(proveedor.ProveedorId))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(proveedor);
        }

        // GET: Proveedores/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proveedor = await _context.Proveedores.FirstOrDefaultAsync(m => m.ProveedorId == id);
            if (proveedor == null)
            {
                return NotFound();
            }

            return View(proveedor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor != null)
            {
                // Actualizar los productos asociados para que no tengan proveedor
                var productos = _context.Productos.Where(p => p.ProveedorId == id).ToList();
                foreach (var producto in productos)
                {
                    producto.ProveedorId = null;
                }

                // Guardar los cambios en los productos
                _context.Productos.UpdateRange(productos);

                // Eliminar el proveedor
                _context.Proveedores.Remove(proveedor);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }


        private bool ProveedorExists(int id)
        {
            return _context.Proveedores.Any(e => e.ProveedorId == id);
        }
    }
}
