using Microsoft.AspNetCore.Mvc;

namespace vivero.Models
{
    public class HistorialActividad
    {
        public int Id { get; set; }
        public string UsuarioId { get; set; }
        public string Accion { get; set; }
        public DateTime FechaHora { get; set; }
        public string Detalles { get; set; }
    }
}
