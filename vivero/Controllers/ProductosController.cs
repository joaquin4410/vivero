using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using vivero.Models;
using System.Threading.Tasks;
using System.IO;
using QRCoder;
using System.Drawing;
using System;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Net.Mail;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;

namespace vivero.Controllers
{
    [Authorize(Roles = "Administrador,Trabajador")]
    public class ProductosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductosController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Productos
        public async Task<IActionResult> Index()
        {
            var productos = await _context.Productos.Include(p => p.Proveedor).ToListAsync();

            // Inicializa ViewBag.AlgunaPropiedad con una estructura válida
            ViewBag.AlgunaPropiedad = new
            {
                SubPropiedad = "Valor por defecto o calculado"
            };

            return View(productos);
        }

        // Ajustes en ProductosController para asegurar que los registros se guarden correctamente
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ProductosDisponibles()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth", new { redirectProductos = 1 });
            }

            var productos = await _context.Productos.Include(p => p.Proveedor).ToListAsync();
            return View("ProductosDisponibles", productos);
        }
        private async Task RegistrarGananciasHistoricas()
        {
            var productos = await _context.Productos.Include(p => p.Ventas).ToListAsync();

            foreach (var producto in productos)
            {
                // Calcula las ganancias para el producto
                decimal totalGanancia = producto.Ventas.Sum(v => v.PrecioVenta * v.CantidadVendida);

                // Comprueba si ya existe un registro histórico para este producto
                var gananciaExistente = await _context.GananciasHistoricas
                    .FirstOrDefaultAsync(g => g.ProductoId == producto.ProductoId && g.FechaRegistro.Date == DateTime.Now.Date);

                if (gananciaExistente == null)
                {
                    // Registrar nueva ganancia histórica
                    var gananciaHistorica = new GananciaHistorica
                    {
                        ProductoId = producto.ProductoId,
                        ProductoNombre = producto.Nombre,
                        TotalGanancia = totalGanancia,
                        FechaRegistro = DateTime.Now
                    };

                    _context.GananciasHistoricas.Add(gananciaHistorica);
                }
                else
                {
                    // Actualizar la ganancia histórica existente
                    gananciaExistente.TotalGanancia = totalGanancia;
                    _context.GananciasHistoricas.Update(gananciaExistente);
                }
            }

            await _context.SaveChangesAsync();
        }


        // Método para buscar productos por Código de Hilera
        [HttpGet]
        public async Task<IActionResult> Search(string codigoHilera)
        {
            var producto = await _context.Productos
                .Include(p => p.Proveedor)
                .FirstOrDefaultAsync(p => p.CodigoHilera.ToString() == codigoHilera);

            if (producto == null)
            {
                return NotFound();
            }

            return View("Details", producto);
        }
        // Catálogo en línea
        [HttpGet]
        public async Task<IActionResult> Catalogo()
        {
            var productos = await _context.Productos.Include(p => p.Proveedor).ToListAsync();
            return View(productos); // Crear una vista Catalogo.cshtml
        }


        // Generación de reportes frecuentes
        [HttpGet]
        public IActionResult GenerarReporteFrecuencia(string frecuencia)
        {
            // Validar frecuencia
            if (string.IsNullOrEmpty(frecuencia) || !(new[] { "diario", "semanal", "mensual" }.Contains(frecuencia.ToLower())))
            {
                ViewBag.Error = "Frecuencia inválida. Usa diario, semanal o mensual.";
                return View("GenerarReporteFrecuencia");
            }

            DateTime fechaInicio;
            DateTime fechaFin = DateTime.Now;

            // Determinar rango de fechas según la frecuencia
            switch (frecuencia.ToLower())
            {
                case "diario":
                    fechaInicio = fechaFin.Date;
                    break;
                case "semanal":
                    fechaInicio = fechaFin.AddDays(-7);
                    break;
                case "mensual":
                    fechaInicio = fechaFin.AddMonths(-1);
                    break;
                default:
                    fechaInicio = fechaFin.Date;
                    break;
            }

            // Generar reporte
            var reportes = _context.Ventas
                .Where(v => v.FechaVenta >= fechaInicio && v.FechaVenta <= fechaFin)
                .GroupBy(v => v.FechaVenta.Date)
                .Select(g => new
                {
                    Fecha = g.Key,
                    TotalVentas = g.Sum(v => v.PrecioVenta * v.CantidadVendida),
                    CantidadProductos = g.Sum(v => v.CantidadVendida)
                })
                .OrderBy(r => r.Fecha)
                .ToList();

            if (!reportes.Any())
            {
                ViewBag.Error = "No se encontraron datos para la frecuencia seleccionada.";
                return View("GenerarReporteFrecuencia");
            }

            ViewBag.Frecuencia = frecuencia;
            return View("GenerarReporteFrecuencia", reportes);
        }


        // Método para generar la URL del código QR
        private string GenerarCodigoQR(int productoId)
        {
            var url = Url.Action("ScanQR", "Productos", new { id = productoId }, protocol: Request.Scheme);
            Console.WriteLine("URL generada para el QR: " + url); // Esto imprime la URL generada
            return url;
        }

        public IActionResult ProductosMasVendidos()
        {
            var productosMasVendidos = _context.Ventas
                .Include(v => v.Producto)
                .GroupBy(v => new { v.ProductoId, v.Producto.Nombre })
                .Select(g => new ProductoMasVendidoDto
                {
                    ProductoId = g.Key.ProductoId,
                    Nombre = g.Key.Nombre,
                    TotalVendido = g.Sum(v => v.CantidadVendida)
                })
                .OrderByDescending(p => p.TotalVendido)
                .Take(10)
                .ToList();

            return View(productosMasVendidos);
        }



        public async Task<IActionResult> Promociones()
        {
            var productos = await _context.Productos.ToListAsync();
            return View(productos);
        }
        public IActionResult AlertaBajoInventario()
        {
            int limiteMinimo = 10; // Define el límite mínimo para alertar
            var productosBajoInventario = _context.Productos
                .Where(p => p.Cantidad <= limiteMinimo)
                .Include(p => p.Proveedor) // Asegúrate de incluir los datos del proveedor si los necesitas
                .ToList();

            if (productosBajoInventario == null || !productosBajoInventario.Any())
            {
                ViewBag.ProductosBajoInventario = new List<Producto>(); // Inicializa con una lista vacía para evitar errores
            }
            else
            {
                ViewBag.ProductosBajoInventario = productosBajoInventario;
            }

            ViewBag.LimiteMinimo = limiteMinimo;
            return View();
        }

        public async Task<IActionResult> Details3(int id)
        {
            var producto = await _context.Productos
                .Include(p => p.Proveedor)
                .FirstOrDefaultAsync(p => p.ProductoId == id);

            if (producto == null)
            {
                return NotFound();
            }

            ViewBag.CantidadRestante = producto.Cantidad;
            ViewBag.CantidadComprada = _context.Ventas
                .Where(v => v.ProductoId == id)
                .Sum(v => v.CantidadVendida);

            return View("Details3", producto); // Cambiar para que apunte a la nueva vista.
        }

        // Aplicar promoción a un producto
        [HttpPost]
        public async Task<IActionResult> AplicarPromocion(int productoId, decimal descuentoPorcentaje)
        {
            if (descuentoPorcentaje < 0 || descuentoPorcentaje > 100)
            {
                ModelState.AddModelError("", "El porcentaje de descuento debe estar entre 0 y 100.");
                return RedirectToAction(nameof(Promociones));
            }

            var producto = await _context.Productos.FindAsync(productoId);

            if (producto == null) return NotFound();

            // Calcular el nuevo precio con el descuento aplicado
            var nuevoPrecio = producto.Precio * (1 - descuentoPorcentaje / 100m);
            producto.Precio = nuevoPrecio;

            _context.Productos.Update(producto);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"La promoción del {descuentoPorcentaje}% se aplicó correctamente al producto '{producto.Nombre}'.";

            return RedirectToAction(nameof(Promociones));
        }

        // Eliminar la promoción (restaurar precio original)
        [HttpPost]
        public async Task<IActionResult> EliminarPromocion(int productoId, decimal precioOriginal)
        {
            var producto = await _context.Productos.FindAsync(productoId);

            if (producto == null) return NotFound();

            // Restaurar el precio original
            producto.Precio = precioOriginal;

            _context.Productos.Update(producto);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Se eliminó la promoción y el producto '{producto.Nombre}' se restauró a su precio original.";

            return RedirectToAction(nameof(Promociones));
        }

        // Método para generar la imagen del código QR
        private string GenerarImagenCodigoQR(string texto)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(texto, QRCodeGenerator.ECCLevel.Q);
                QRCode qrCode = new QRCode(qrCodeData);

                using (Bitmap bitmap = qrCode.GetGraphic(20))
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        return "data:image/png;base64," + Convert.ToBase64String(stream.ToArray());
                    }
                }
            }
        }

        public IActionResult HistorialMovimientos()
        {
            var movimientos = _context.MovimientosStock
                .Include(m => m.Producto) // Incluye la relación con Producto
                .ToList();

            return View(movimientos);
        }
        public IActionResult HistorialTransacciones(int proveedorId)
        {
            var transacciones = _context.Ventas
                .Include(v => v.Producto)
                .Where(v => v.Producto.ProveedorId == proveedorId)
                .ToList();

            return View(transacciones);
        }
        // GET: Productos/Create
        public IActionResult Create()
        {
            ViewBag.Proveedores = _context.Proveedores.ToList();
            return View();
        }

        // POST: Productos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string Nombre, string Descripcion, decimal Precio, int Cantidad, IFormFile Foto, string Estado, string Categoria, int proveedorId)
        {
            // Validación de datos
            if (Precio < 0 || Cantidad < 0)
            {
                ModelState.AddModelError("", "El precio y la cantidad no pueden ser negativos.");
                ViewBag.Proveedores = _context.Proveedores.ToList();
                return View();
            }

            if (string.IsNullOrEmpty(Categoria))
            {
                ModelState.AddModelError("", "La categoría del producto es obligatoria.");
                ViewBag.Proveedores = _context.Proveedores.ToList();
                return View();
            }

            if (ModelState.IsValid)
            {
                Random random = new Random();
                int codigoHilera = random.Next(100000, 999999);

                // Manejo de imagen
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = null;
                if (Foto != null && Foto.Length > 0)
                {
                    uniqueFileName = Guid.NewGuid().ToString() + "_" + Foto.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await Foto.CopyToAsync(fileStream);
                    }
                }

                // Creación del objeto Producto
                var producto = new Producto
                {
                    Nombre = Nombre,
                    Descripcion = Descripcion,
                    Precio = Precio,
                    Cantidad = Cantidad,
                    Foto = uniqueFileName != null ? "/uploads/" + uniqueFileName : null,
                    Estado = Estado,
                    Categoria = Categoria,
                    FechaIngreso = DateTime.Now,
                    CodigoHilera = codigoHilera,
                    ProveedorId = proveedorId
                };

                // Guardar el producto en la base de datos
                _context.Productos.Add(producto);
                await _context.SaveChangesAsync();

                // Registrar actividad
                await RegistrarActividad(User.Identity.Name, "Crear Producto", $"Producto creado: {producto.Nombre}");

                // Generar la URL para el código QR
                var qrUrl = GenerarCodigoQR(producto.ProductoId);
                Console.WriteLine("URL generada para el QR en Create: " + qrUrl);

                // Pasar la URL generada a GenerarImagenCodigoQR
                producto.CodigoQR = GenerarImagenCodigoQR(qrUrl);

                _context.Productos.Update(producto);
                await _context.SaveChangesAsync();

                ViewBag.CodigoHileraGenerado = codigoHilera;

                return RedirectToAction("Index");
            }

            // En caso de error, recargar la lista de proveedores
            ViewBag.Proveedores = _context.Proveedores.ToList();
            return View();
        }



        // GET: Productos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var producto = await _context.Productos.Include(p => p.Proveedor).FirstOrDefaultAsync(p => p.ProductoId == id);
            if (producto == null)
            {
                return NotFound();
            }

            var gananciasPorPlanta = await CalcularGananciasPorPlanta(id);
            var gananciasMensuales = await CalcularGananciasMensuales();

            ViewBag.GananciasPorPlanta = gananciasPorPlanta;
            ViewBag.GananciasMensuales = gananciasMensuales;
            ViewBag.PrecioCompra = producto.Proveedor?.PrecioCompra;

            return View(producto);
        }
        private async Task VerificarYEnviarAlertaDeStockBajo()
        {
            int limiteMinimo = 10; // Límite mínimo de stock para enviar alertas
            var productosBajoInventario = await _context.Productos
                .Where(p => p.Cantidad <= limiteMinimo)
                .Include(p => p.Proveedor) // Asegúrate de incluir el proveedor relacionado
                .ToListAsync();

            if (productosBajoInventario.Any())
            {
                foreach (var producto in productosBajoInventario)
                {
                    // Preparar el mensaje del correo
                    string mensaje = $"El producto '{producto.Nombre}' está próximo a agotarse con un stock de {producto.Cantidad} unidades. " +
                                     $"Por favor, contacta al proveedor '{producto.Proveedor?.Nombre}' para reabastecer.";

                    // Enviar el correo al encargado del vivero
                    string correoDestino = "moralesbjoaquin@gmail.com"; // Correo fijo del encargado
                    await EnviarCorreoAlerta(correoDestino, "Alerta de Stock Bajo", mensaje);
                }
            }
        }

        private async Task EnviarCorreoAlerta(string correoDestino, string asunto, string mensaje)
        {
            using (var mail = new MailMessage())
            {
                mail.From = new MailAddress("ncjvivero@gmail.com", "Vivero NCJ");
                mail.To.Add(correoDestino);
                mail.Subject = asunto;
                mail.Body = mensaje;

                using (var smtp = new SmtpClient("smtp.gmail.com"))
                {
                    smtp.Credentials = new System.Net.NetworkCredential("ncjvivero@gmail.com", "ivrzgkeietuvqbcu");
                    smtp.Port = 587;
                    smtp.EnableSsl = true;

                    await smtp.SendMailAsync(mail);
                }
            }
        }

        // Método para manejar la venta de productos
        [HttpPost] 
[ValidateAntiForgeryToken]
public async Task<IActionResult> Vender(int id, int cantidad, string cuidados, string correoCliente)
{
    var producto = await _context.Productos.FindAsync(id);
    if (producto == null || producto.Cantidad < cantidad)
    {
        return BadRequest("Cantidad no disponible o producto no encontrado.");
    }

    // Reducir el stock del producto
    producto.Cantidad -= cantidad;

    // Registrar la venta
    var venta = new Venta
    {
        ProductoId = id,
        CantidadVendida = cantidad,
        PrecioVenta = producto.Precio,
        TotalVenta = cantidad * producto.Precio,
        FechaVenta = DateTime.Now
    };
    _context.Ventas.Add(venta);

    // Registrar el movimiento de stock
    var movimiento = new MovimientoStock
    {
        ProductoId = id,
        TipoMovimiento = "Salida",
        Cantidad = cantidad,
        Fecha = DateTime.Now
    };
    _context.MovimientosStock.Add(movimiento);

    // Registrar las ganancias en el historial, independiente del estado del producto
    var gananciaHistorica = new GananciaHistorica
    {
        ProductoId = producto.ProductoId,
        ProductoNombre = producto.Nombre,
        TotalGanancia = cantidad * producto.Precio,
        FechaRegistro = DateTime.Now
    };
    _context.GananciasHistoricas.Add(gananciaHistorica);

    // Verificar si la cantidad llegó a cero
    if (producto.Cantidad == 0)
    {
        // Registrar el historial antes de eliminar
        var historial = new HistorialActividad
        {
            UsuarioId = User.Identity.Name,
            Accion = "Eliminar Producto",
            Detalles = $"Producto eliminado automáticamente: {producto.Nombre} (ID: {producto.ProductoId}) tras agotar su stock.",
            FechaHora = DateTime.Now
        };
        _context.HistorialActividades.Add(historial);

        // Eliminar el producto
        _context.Productos.Remove(producto);
    }
    else
    {
        _context.Productos.Update(producto);
    }

    // Guardar los cambios
    await _context.SaveChangesAsync();

    // Generar QR
    var qrUrl = Url.Action("ScanQR", "Productos", new { id = producto.ProductoId, cuidados = cuidados }, protocol: Request.Scheme);
    var boletaQR = GenerarImagenCodigoQR(qrUrl);
    ViewBag.BoletaQR = boletaQR;

    // Generar el PDF
    var pdfBytes = GenerarBoletaPDF(venta, producto, cuidados);
    ViewBag.BoletaPDF = Convert.ToBase64String(pdfBytes);

    // Intentar enviar el correo
    try
    {
        await EnviarCorreoConBoleta(correoCliente, venta, producto, cuidados);
        TempData["Success"] = "El correo fue enviado correctamente.";
    }
    catch (Exception ex)
    {
        TempData["Error"] = "Hubo un problema al enviar el correo: " + ex.Message;
    }

    await VerificarYEnviarAlertaDeStockBajo();

    return View("Boleta", venta);
}


        // Generación de PDF de boleta
        private byte[] GenerarBoletaPDF(Venta venta, Producto producto, string cuidados)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4, 36, 36, 36, 36);
                PdfWriter writer = PdfWriter.GetInstance(doc, stream);
                doc.Open();

                // Título de la boleta
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                doc.Add(new Paragraph("Vivero NCJ - Boleta de Venta", titleFont));
                doc.Add(new Paragraph($"Fecha de emisión: {DateTime.Now.ToShortDateString()}", smallFont));
                doc.Add(new Paragraph($"Boleta N°: 0001-{producto.ProductoId}", smallFont));
                doc.Add(new Paragraph(" ", normalFont)); // Espacio

                // Detalles de la compra
                PdfPTable table = new PdfPTable(2);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 30, 70 }); // Ajustar ancho de columnas
                table.AddCell(new PdfPCell(new Phrase("Producto:", normalFont)) { Border = 0 });
                table.AddCell(new PdfPCell(new Phrase(producto.Nombre, normalFont)) { Border = 0 });
                table.AddCell(new PdfPCell(new Phrase("Cantidad Vendida:", normalFont)) { Border = 0 });
                table.AddCell(new PdfPCell(new Phrase(venta.CantidadVendida.ToString(), normalFont)) { Border = 0 });
                table.AddCell(new PdfPCell(new Phrase("Precio Unitario:", normalFont)) { Border = 0 });
                table.AddCell(new PdfPCell(new Phrase(producto.Precio.ToString("C0", new System.Globalization.CultureInfo("es-CL")), normalFont)) { Border = 0 });
                table.AddCell(new PdfPCell(new Phrase("Total Venta:", normalFont)) { Border = 0 });
                table.AddCell(new PdfPCell(new Phrase((venta.CantidadVendida * producto.Precio).ToString("C0", new System.Globalization.CultureInfo("es-CL")), normalFont)) { Border = 0 });
                table.AddCell(new PdfPCell(new Phrase("Cuidados:", normalFont)) { Border = 0 });
                table.AddCell(new PdfPCell(new Phrase(cuidados, normalFont)) { Border = 0 });

                doc.Add(table);

                // Espacio
                doc.Add(new Paragraph(" ", normalFont));

                // Código QR
                if (!string.IsNullOrEmpty(ViewBag.BoletaQR))
                {
                    // Aquí especificamos explícitamente iTextSharp.text.Image
                    var qrImage = iTextSharp.text.Image.GetInstance(Convert.FromBase64String(ViewBag.BoletaQR.Replace("data:image/png;base64,", "")));
                    qrImage.ScaleAbsolute(100, 100); // Ajustar tamaño del QR
                    qrImage.Alignment = Element.ALIGN_CENTER;
                    doc.Add(qrImage);
                }

                // Pie de página
                doc.Add(new Paragraph("Gracias por su compra", smallFont));
                doc.Add(new Paragraph("Vivero NCJ - Generado automáticamente", smallFont));

                doc.Close();
                return stream.ToArray();
            }
        }



        [HttpGet("Productos/ScanQR/{id}")]
        [AllowAnonymous] // Permitir acceso a todos
        public async Task<IActionResult> ScanQR(int id, string cuidados)
        {
            var producto = await _context.Productos.FindAsync(id);

            if (producto == null)
            {
                return NotFound();
            }

            // Si los cuidados vienen en la URL, pásalos a ViewBag
            ViewBag.Cuidados = cuidados;

            // Verifica si el usuario está autenticado y personaliza el contenido si es necesario
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.Message = "Bienvenido, " + User.Identity.Name;
            }
            else
            {
                ViewBag.Message = "Por favor, inicia sesión para acceder a más funciones.";
            }

            return View(producto); // Esto buscará automáticamente ScanQR.cshtml
        }



        // Envío de correo con la boleta en PDF
        private async Task EnviarCorreoConBoleta(string correo, Venta venta, Producto producto, string cuidados)
        {
            byte[] pdfBytes = GenerarBoletaPDF(venta, producto, cuidados);

            using (var mail = new MailMessage())
            {
                mail.From = new MailAddress("ncjvivero@gmail.com", "Vivero NCJ");
                mail.To.Add(correo);
                mail.Subject = "Boleta de Venta";
                mail.Body = "Adjunto encontrarás la boleta de tu compra.";
                mail.Attachments.Add(new Attachment(new MemoryStream(pdfBytes), "boleta.pdf"));

                using (var smtp = new SmtpClient("smtp.gmail.com"))
                {
                    smtp.Credentials = new System.Net.NetworkCredential("ncjvivero@gmail.com", "ivrzgkeietuvqbcu");
                    smtp.Port = 587;
                    smtp.EnableSsl = true;

                    await smtp.SendMailAsync(mail);
                }
            }
        }
        // GET: Productos/GestionPromociones
        public async Task<IActionResult> GestionPromociones()
        {
            var promociones = await _context.Promociones
                .Include(p => p.Producto)
                .ToListAsync();
            return View(promociones);
        }

        // POST: Productos/CrearPromocion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearPromocion(int productoId, decimal descuento, DateTime fechaInicio, DateTime fechaFin)
        {
            if (descuento <= 0 || descuento > 100)
            {
                ModelState.AddModelError("", "El descuento debe estar entre 1% y 100%.");
                return RedirectToAction(nameof(GestionPromociones));
            }

            var promocion = new Promocion
            {
                ProductoId = productoId,
                Descuento = descuento,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            };

            _context.Promociones.Add(promocion);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Promoción creada correctamente.";
            return RedirectToAction(nameof(GestionPromociones));
        }

        // GET: Productos/Vender
        public IActionResult Vender(int id)
        {
            ViewBag.ProductoId = id;
            return View();
        }

        // GET: Productos/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var producto = await _context.Productos.Include(p => p.Proveedor).FirstOrDefaultAsync(p => p.ProductoId == id);
            if (producto == null)
            {
                return NotFound();
            }
            return View(producto); // Asegúrate de que tienes una vista Delete.cshtml para confirmar la eliminación.
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto != null)
            {
                try
                {
                    // Registrar las ganancias históricas solo si la tabla está disponible
                    if (_context.Database.CanConnect() && _context.Model.FindEntityType(typeof(GananciaHistorica)) != null)
                    {
                        var gananciaHistorica = new GananciaHistorica
                        {
                            ProductoId = producto.ProductoId,
                            ProductoNombre = producto.Nombre,
                            TotalGanancia = producto.Precio * producto.Cantidad,
                            FechaRegistro = DateTime.Now
                        };

                        _context.GananciasHistoricas.Add(gananciaHistorica);
                    }
                    else
                    {
                        // Log para indicar que la tabla no está disponible
                        Console.WriteLine("La tabla 'GananciasHistoricas' no existe. Se omite el registro de ganancias históricas.");
                    }

                    // Eliminar el producto
                    _context.Productos.Remove(producto);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al intentar eliminar el producto: {ex.Message}");
                    // Manejar errores adicionales aquí si es necesario
                    return RedirectToAction(nameof(Index), new { error = "Error al eliminar el producto." });
                }
            }
            return RedirectToAction(nameof(Index));
        }




        [HttpGet]
        public async Task<IActionResult> Buscar(string termino)
        {
            // Verifica si el término está vacío o nulo
            if (string.IsNullOrEmpty(termino))
            {
                ViewBag.ErrorMessage = "Debe ingresar un término de búsqueda.";
                return View(new List<Producto>());
            }

            // Consulta los productos que coinciden con el término
            var productos = await _context.Productos
                .Include(p => p.Proveedor) // Incluye los datos del proveedor
                .Where(p => p.Nombre.Contains(termino) || p.Descripcion.Contains(termino))
                .ToListAsync();

            // Si no se encontraron productos, mostrar un mensaje
            if (!productos.Any())
            {
                ViewBag.ErrorMessage = "No se encontraron productos para el término ingresado.";
            }

            // Retorna la vista con los productos encontrados
            return View(productos);
        }

        [HttpGet]
        public async Task<IActionResult> FiltrarPorCategoria(string categoria)
        {
            if (string.IsNullOrEmpty(categoria))
            {
                ViewBag.ErrorMessage = "Debe seleccionar una categoría.";
                return RedirectToAction(nameof(Catalogo));
            }

            var productos = await _context.Productos
                .Include(p => p.Proveedor)
                .Where(p => p.Categoria == categoria)
                .ToListAsync();

            if (!productos.Any())
            {
                ViewBag.ErrorMessage = $"No se encontraron productos en la categoría '{categoria}'.";
            }

            ViewBag.CategoriaSeleccionada = categoria;
            return View("Catalogo", productos);
        }


        // GET: Productos/InformacionStock
        [HttpGet]
        public async Task<IActionResult> InformacionStock()
        {
            var stock = await _context.Productos
                .Include(p => p.Proveedor) // Incluye los datos del proveedor
                .Select(p => new StockViewModel
                {
                    ProductoNombre = p.Nombre,
                    Cantidad = p.Cantidad,
                    Categoria = p.Categoria, // Llena la nueva propiedad
                    ProveedorNombre = p.Proveedor.Nombre
                })
                .ToListAsync();

            return View(stock);
        }


        private async Task RegistrarActividad(string usuarioId, string accion, string detalles)
        {
            var actividad = new HistorialActividad
            {
                UsuarioId = usuarioId,
                Accion = accion,
                FechaHora = DateTime.Now,
                Detalles = detalles
            };

            _context.HistorialActividades.Add(actividad);
            await _context.SaveChangesAsync();
        }

        private async Task<decimal> CalcularGananciasPorPlanta(int productoId)
        {
            var ventas = await _context.Ventas
                .Where(v => v.ProductoId == productoId)
                .ToListAsync();

            decimal totalGanancias = ventas.Sum(v => v.PrecioVenta * v.CantidadVendida);
            return totalGanancias;
        }

        private async Task<decimal> CalcularGananciasMensuales()
        {
            var inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var ventasMensuales = await _context.Ventas
                .Where(v => v.FechaVenta >= inicioMes)
                .ToListAsync();

            decimal totalGananciasMensuales = ventasMensuales.Sum(v => v.PrecioVenta * v.CantidadVendida);
            return totalGananciasMensuales;
        }

        private bool ProductoExists(int id)
        {
            return _context.Productos.Any(e => e.ProductoId == id);
        }
    }
}
