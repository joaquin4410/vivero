using Microsoft.AspNetCore.Mvc;

namespace vivero.Models
{
    public class StockViewModel
    {
        public string ProductoNombre { get; set; } // Nombre del producto
        public int Cantidad { get; set; } // Cantidad en stock
        public string Categoria { get; set; } // Nueva propiedad
        public string ProveedorNombre { get; set; } // Nombre del proveedor
    }
}
