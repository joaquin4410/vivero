using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace vivero.Models
{
    public class Promocion
    {
        public int PromocionId { get; set; } // O cualquier otro nombre que funcione como identificador
        public int ProductoId { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal Descuento { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public Producto Producto { get; set; } // Relación con Producto
    }
}
