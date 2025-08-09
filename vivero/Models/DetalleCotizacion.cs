using Microsoft.AspNetCore.Mvc;

namespace vivero.Models
{
    public class DetalleCotizacion
    {
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal PrecioTotal { get; set; }
    }
}
