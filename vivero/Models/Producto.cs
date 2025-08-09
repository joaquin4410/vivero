using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace vivero.Models
{
    public class Producto
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
        public string Foto { get; set; }
        public string Categoria { get; set; }
        public string Estado { get; set; }
        public DateTime FechaIngreso { get; set; }
        public string? CodigoQR { get; set; } // Para almacenar la imagen del código QR
        public int CodigoHilera { get; set; } // Para el código de 6 dígitos
        public int? ProveedorId { get; set; }// Clave foránea al proveedor
        public Proveedor Proveedor { get; set; } // Relación con el proveedor
        public ICollection<Venta> Ventas { get; set; }

    }

}
