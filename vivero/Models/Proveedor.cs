using System.ComponentModel.DataAnnotations;

namespace vivero.Models
{
    public class Proveedor
    {
        [Key]
        public int ProveedorId { get; set; }

        [Required(ErrorMessage = "El nombre del proveedor es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres.")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El precio de compra es obligatorio.")]
        [Range(0, double.MaxValue, ErrorMessage = "El precio de compra no puede ser negativo.")]
        [DataType(DataType.Currency)]
        public decimal PrecioCompra { get; set; }
    }
}
