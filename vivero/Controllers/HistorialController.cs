using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using vivero.Models;

namespace vivero.Controllers
{
    [Authorize(Roles = "Administrador, Trabajador")]
    public class HistorialController : Controller
    {
        private readonly AppDbContext _context;

        public HistorialController(AppDbContext context)
        {
            _context = context;
        }
        [Authorize(Roles = "Administrador, Trabajador")]
        public async Task<IActionResult> Index()
        {
            var actividades = _context.HistorialActividades
                .OrderByDescending(a => a.FechaHora)
                .ToList();

            return View(actividades);
        }
    }
}
