using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace vivero.Models
{
    public class GananciaMensual
    {
        [Key]
        public int Id { get; set; }
        public int Año { get; set; }
        public int Mes { get; set; }
        public int Dia { get; set; }
        public decimal TotalGanancias { get; set; }
    }
}
