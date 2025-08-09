using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace vivero.Models
{
    public class VentasPorDiaDto
    {
        public DateTime Fecha { get; set; } // Cambiado a DateTime
        public decimal Total { get; set; }
    }

}
