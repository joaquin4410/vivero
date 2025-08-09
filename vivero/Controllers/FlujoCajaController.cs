using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using vivero.Models;
using Microsoft.AspNetCore.Authorization;

namespace vivero.Controllers
{
    [Authorize(Roles = "Administrador, Trabajador")]
    public class FlujoCajaController : Controller
    {
        private readonly AppDbContext _context;

        public FlujoCajaController(AppDbContext context)
        {
            _context = context;
        }

        // GET: FlujoCaja
        public async Task<IActionResult> Index()
        {
            var flujoCajas = await _context.FlujoCajas.ToListAsync();
            return View(flujoCajas);
        }

        // GET: FlujoCaja/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var flujoCaja = await _context.FlujoCajas.FindAsync(id);
            if (flujoCaja == null)
            {
                return NotFound();
            }
            return View(flujoCaja);
        }

        // GET: FlujoCaja/Create
        public IActionResult Create()
        {
            // Si necesitas cargar alguna información adicional para la vista, la pasas aquí.
            return View();
        }

        // POST: FlujoCaja/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DateTime Fecha, decimal SaldoInicial, decimal VentasEfectivo, decimal VentasCredito, decimal CobrosActivos, decimal CompraMercancia, decimal PagoNomina, decimal PagoProveedores, decimal PagoImpuestos, decimal PrestamosRecibidos)
        {
            if (SaldoInicial < 0 || VentasEfectivo < 0 || VentasCredito < 0 || CobrosActivos < 0 || CompraMercancia < 0 || PagoNomina < 0 || PagoProveedores < 0 || PagoImpuestos < 0 || PrestamosRecibidos < 0)
            {
                ModelState.AddModelError("", "No se permiten valores negativos.");
                return View();
            }
            if (ModelState.IsValid)
            {
                var flujoCaja = new FlujoCaja
                {
                    Fecha = Fecha,
                    SaldoInicial = SaldoInicial,
                    VentasEfectivo = VentasEfectivo,
                    VentasCredito = VentasCredito,
                    CobrosActivos = CobrosActivos,
                    CompraMercancia = CompraMercancia,
                    PagoNomina = PagoNomina,
                    PagoProveedores = PagoProveedores,
                    PagoImpuestos = PagoImpuestos,
                    PrestamosRecibidos = PrestamosRecibidos
                };

                _context.FlujoCajas.Add(flujoCaja);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View();
        }

        // GET: FlujoCaja/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var flujoCaja = await _context.FlujoCajas.FindAsync(id);
            if (flujoCaja == null)
            {
                return NotFound();
            }
            return View(flujoCaja);
        }

        // POST: FlujoCaja/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FlujoCaja flujoCaja)
        {
            if (id != flujoCaja.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(flujoCaja);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FlujoCajaExists(flujoCaja.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(flujoCaja);
        }

        // GET: FlujoCaja/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var flujoCaja = await _context.FlujoCajas.FindAsync(id);
            if (flujoCaja == null)
            {
                return NotFound();
            }
            return View(flujoCaja);
        }

        // POST: FlujoCaja/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var flujoCaja = await _context.FlujoCajas.FindAsync(id);
            _context.FlujoCajas.Remove(flujoCaja);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FlujoCajaExists(int id)
        {
            return _context.FlujoCajas.Any(e => e.Id == id);
        }
    }
}
