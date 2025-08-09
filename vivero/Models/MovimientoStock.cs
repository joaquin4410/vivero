using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace vivero.Models
{
    public class MovimientoStock
    {
        [Key]
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public Producto Producto { get; set; }
        public string TipoMovimiento { get; set; } // Entrada o Salida
        public int Cantidad { get; set; }
        public DateTime Fecha { get; set; }
    }
}
