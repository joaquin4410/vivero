using Microsoft.AspNetCore.Mvc;

namespace vivero.Models
{
    public class ProductoMasVendidoDto
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; }
        public int TotalVendido { get; set; }
    }
}
