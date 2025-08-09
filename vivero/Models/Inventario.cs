using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace vivero.Models
{
    public class Inventario
    {
        [Key]
        public int Id { get; set; }

        public int ProductoId { get; set; }  // Llave foránea hacia Producto
        public Producto Producto { get; set; }  // Relación con Producto

        public int Cantidad { get; set; }  // Cantidad en inventario
    }
}