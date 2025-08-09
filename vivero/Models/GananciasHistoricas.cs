using Microsoft.AspNetCore.Mvc;

namespace vivero.Models
{
    public class GananciaHistorica
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public string ProductoNombre { get; set; }
        public decimal TotalGanancia { get; set; }
        public DateTime FechaRegistro { get; set; }
    }

}
