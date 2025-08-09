using Microsoft.EntityFrameworkCore;
using vivero.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<MovimientoStock> MovimientosStock { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Producto> Productos { get; set; }
    public DbSet<Venta> Ventas { get; set; }
    public DbSet<GananciaMensual> GananciasMensuales { get; set; }
    public DbSet<Proveedor> Proveedores { get; set; }
    public DbSet<FlujoCaja> FlujoCajas { get; set; }
    public DbSet<Promocion> Promociones { get; set; }
    public DbSet<HistorialActividad> HistorialActividades { get; set; }
    public DbSet<GananciaHistorica> GananciasHistoricas { get; set; }
    public DbSet<PasswordResetRequest> PasswordResetRequests { get; set; }
    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>()
            .Property(p => p.Precio)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Producto>()
            .HasMany(p => p.Ventas)
            .WithOne(v => v.Producto)
            .HasForeignKey(v => v.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configuración para Venta
        modelBuilder.Entity<Venta>()
            .Property(v => v.PrecioVenta)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Venta>()
            .Property(v => v.TotalVenta)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Venta>()
            .HasOne(v => v.FlujoCaja)
            .WithMany(fc => fc.Ventas)
            .HasForeignKey(v => v.FlujoCajaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configuración para GananciaMensual
        modelBuilder.Entity<GananciaMensual>()
            .Property(g => g.TotalGanancias)
            .HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Venta>()
        .HasOne(v => v.Producto)
        .WithMany(p => p.Ventas)
        .HasForeignKey(v => v.ProductoId)
        .OnDelete(DeleteBehavior.Cascade);

        // Configuración para Proveedor
        modelBuilder.Entity<Proveedor>()
            .Property(p => p.PrecioCompra)
            .HasColumnType("decimal(18,2)");

        // Configuración para FlujoCaja
        modelBuilder.Entity<FlujoCaja>()
            .Property(fc => fc.SaldoInicial).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<FlujoCaja>()
            .Property(fc => fc.VentasEfectivo).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<FlujoCaja>()
            .Property(fc => fc.VentasCredito).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<FlujoCaja>()
            .Property(fc => fc.CobrosActivos).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<FlujoCaja>()
            .Property(fc => fc.CompraMercancia).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<FlujoCaja>()
            .Property(fc => fc.PagoNomina).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<FlujoCaja>()
            .Property(fc => fc.PagoProveedores).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<FlujoCaja>()
            .Property(fc => fc.PagoImpuestos).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<FlujoCaja>()
            .Property(fc => fc.PrestamosRecibidos).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Promocion>(entity =>
        {
            entity.Property(e => e.Descuento)
                .HasColumnType("decimal(10,2)"); // 10 dígitos en total, 2 decimales
        });

        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Promocion>(entity =>
        {
            entity.Property(e => e.Descuento)
                .HasPrecision(10, 2); // 10 dígitos en total, 2 decimales
        });
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PasswordResetRequest>()
            .Property(p => p.Token)
            .IsRequired();

        modelBuilder.Entity<PasswordResetRequest>()
            .Property(p => p.Email)
            .IsRequired();

        base.OnModelCreating(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<GananciaHistorica>()
            .Property(g => g.TotalGanancia)
            .HasColumnType("decimal(18,2)");
    }

}
