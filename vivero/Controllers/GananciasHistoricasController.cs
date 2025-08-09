using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using vivero.Models;

namespace vivero.Controllers
{
    [Authorize(Roles = "Administrador, Trabajador")]
    public class GananciasHistoricasController : Controller
    {
        private readonly AppDbContext _context;

        public GananciasHistoricasController(AppDbContext context)
        {
            _context = context;
        }

        // Acción para listar las ganancias históricas
        [Authorize(Roles = "Administrador, Trabajador")]
        public async Task<IActionResult> Index()
        {
            var gananciasHistoricas = await _context.GananciasHistoricas
                .OrderByDescending(g => g.FechaRegistro) // Ordenar por las más recientes
                .ToListAsync();
            return View(gananciasHistoricas);
        }
        [Authorize(Roles = "Administrador, Trabajador")]
        // Acción para detalles de una ganancia histórica
        public async Task<IActionResult> Details(int id)
        {
            var gananciaHistorica = await _context.GananciasHistoricas.FindAsync(id);
            if (gananciaHistorica == null)
            {
                return NotFound();
            }
            return View(gananciaHistorica);
        }

        // Acción para eliminar una ganancia histórica
        [Authorize(Roles = "Administrador, Trabajador")]
        public async Task<IActionResult> Delete(int id)
        {
            var gananciaHistorica = await _context.GananciasHistoricas.FindAsync(id);
            if (gananciaHistorica == null)
            {
                return NotFound();
            }
            return View(gananciaHistorica);
        }
        [Authorize(Roles = "Administrador, Trabajador")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var gananciaHistorica = await _context.GananciasHistoricas.FindAsync(id);
            if (gananciaHistorica != null)
            {
                _context.GananciasHistoricas.Remove(gananciaHistorica);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
