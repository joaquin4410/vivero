using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace vivero.Models
{
    public class FlujoCaja
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        // Saldo inicial
        [Column(TypeName = "decimal(18,2)")]
        public decimal SaldoInicial { get; set; }

        // Ingresos
        [Column(TypeName = "decimal(18,2)")]
        public decimal VentasEfectivo { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal VentasCredito { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CobrosActivos { get; set; }

        [NotMapped]
        public decimal TotalIngresos => VentasEfectivo + VentasCredito + CobrosActivos;

        // Relación con Ventas
        public ICollection<Venta> Ventas { get; set; }

        // Egresos
        [Column(TypeName = "decimal(18,2)")]
        public decimal CompraMercancia { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PagoNomina { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PagoProveedores { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PagoImpuestos { get; set; }

        [NotMapped]
        public decimal TotalEgresos => CompraMercancia + PagoNomina + PagoProveedores + PagoImpuestos;

        // Financiamiento
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrestamosRecibidos { get; set; }

        [NotMapped]
        public decimal TotalFinanciamiento => PrestamosRecibidos;

        // Saldo final
        [NotMapped]
        public decimal SaldoFinal => SaldoInicial + TotalIngresos - TotalEgresos + TotalFinanciamiento;
    }
}
