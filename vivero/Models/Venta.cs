using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace vivero.Models
{
    public class Venta
    {
        [Key]
        public int VentaId { get; set; }

        public int ProductoId { get; set; }
        public Producto Producto { get; set; }

        public int CantidadVendida { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioVenta { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalVenta { get; set; }

        public DateTime FechaVenta { get; set; }

        // Relación con FlujoCaja
        public int? FlujoCajaId { get; set; }
        public FlujoCaja FlujoCaja { get; set; }
    }
}
