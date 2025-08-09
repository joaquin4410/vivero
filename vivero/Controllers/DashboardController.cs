using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using vivero.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Globalization;
using System.Text;

namespace vivero.Controllers
{
    [Authorize(Roles = "Administrador, Trabajador")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public DashboardController(AppDbContext context, IConfiguration configuration, HttpClient httpClient)
        {
            _context = context;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<IActionResult> Index()
        {
            // Datos actuales combinados con datos históricos
            var ventasTotales = await _context.GananciasHistoricas.SumAsync(g => (decimal?)g.TotalGanancia) ?? 0;
            var gananciasMensuales = await _context.GananciasHistoricas
                .Where(g => g.FechaRegistro.Month == DateTime.Now.Month && g.FechaRegistro.Year == DateTime.Now.Year)
                .SumAsync(g => (decimal?)g.TotalGanancia) ?? 0;

            var productosInventario = _context.Productos.Count();
            var proveedoresActivos = _context.Proveedores.Count();

            // Actualizar ViewBag
            ViewBag.VentasTotales = ventasTotales;
            ViewBag.GananciasMensuales = gananciasMensuales;
            ViewBag.ProductosInventario = productosInventario;
            ViewBag.ProveedoresActivos = proveedoresActivos;
            var weatherData = await GetWeatherDataAsync();
            ViewBag.Weather = weatherData ?? new WeatherData
            {
                Name = "No disponible",
                Main = new WeatherMain { Temp = 0, Feels_like = 0, Humidity = 0 },
                Weather = new List<WeatherDescription> { new WeatherDescription { Main = "N/A", Description = "No disponible" } }
            };

            // Generar alertas específicas según el clima
            var alertasClima = new List<string>();

            if (weatherData != null)
            {
                var temp = weatherData.Main.Temp;
                var humidity = weatherData.Main.Humidity;
                var description = weatherData.Weather.FirstOrDefault()?.Description;
                string mensajeClima = "";

                if (temp < 5)
                {
                    mensajeClima = "Precaución: Las temperaturas son muy bajas. Protege las plantas sensibles al frío.";
                }
                else if (temp > 30)
                {
                    mensajeClima = "Precaución: Las temperaturas son muy altas. Asegúrate de regar las plantas temprano para evitar la evaporación rápida.";
                }
                else if (humidity < 30)
                {
                    mensajeClima = "Precaución: La humedad es muy baja. Riega las plantas con más frecuencia.";
                }
                else if (humidity > 80)
                {
                    mensajeClima = "Precaución: La humedad es alta. Vigila las plantas por posibles hongos.";
                }
                else if (description != null && description.Contains("lluvia"))
                {
                    mensajeClima = "Precaución: Está lloviendo. Asegúrate de evitar acumulaciones de agua que puedan dañar las raíces.";
                }
                else
                {
                    mensajeClima = "El clima actual no requiere precauciones específicas.";
                }

                ViewBag.MensajeClima = mensajeClima;
            }
            else
            {
                ViewBag.MensajeClima = "No se pudo obtener el clima actual. Verifica tu conexión.";
            }

            ViewBag.AlertasClima = alertasClima;

            // Recomendaciones basadas en clima y estación (ya implementado)
            var recomendaciones = GetRecomendaciones(weatherData);
            ViewBag.Recomendaciones = recomendaciones;
            // Asegurar otros valores necesarios
            ViewBag.EstacionActual = ObtenerEstacion(DateTime.Now);
            ViewBag.Recomendaciones = GetRecomendaciones(null); // Pasa null o los datos climáticos reales

            return View();
        }

        public async Task<IActionResult> CalcularUtilidades(int productoId)
        {
            const decimal IVA = 0.19m; // 19%
            const decimal IMPUESTO_GANANCIAS = 0.25m; // 25%
            const decimal COSTOS_ADICIONALES = 100m; // Costos fijos por unidad

            // Cargar productos para el desplegable
            ViewBag.Productos = await _context.Productos.ToListAsync();

            if (productoId == 0)
            {
                ViewBag.Error = "Selecciona un producto para calcular las utilidades.";
                return View();
            }

            // Buscar el producto en la base de datos
            var producto = await _context.Productos
                .Include(p => p.Proveedor)
                .FirstOrDefaultAsync(p => p.ProductoId == productoId);

            if (producto == null)
            {
                ViewBag.Error = "Producto no encontrado.";
                return View();
            }

            decimal precioConIVA = producto.Precio; // Precio total con IVA
            decimal montoIVA = precioConIVA * IVA; // Monto del IVA
            decimal precioSinIVA = precioConIVA - montoIVA; // Precio base sin IVA
            decimal utilidadBruta = precioSinIVA - COSTOS_ADICIONALES; // Utilidad antes de impuestos
            decimal impuestoGanancias = utilidadBruta * IMPUESTO_GANANCIAS; // Impuesto sobre las ganancias
            decimal utilidadNeta = utilidadBruta - impuestoGanancias; // Utilidad después de impuestos

            // Pasar los resultados a la vista
            ViewBag.Producto = producto;
            ViewBag.PrecioConIVA = precioConIVA;
            ViewBag.PrecioSinIVA = precioSinIVA;
            ViewBag.MontoIVA = montoIVA;
            ViewBag.CostosAdicionales = COSTOS_ADICIONALES;
            ViewBag.UtilidadBruta = utilidadBruta;
            ViewBag.ImpuestoGanancias = impuestoGanancias;
            ViewBag.UtilidadNeta = utilidadNeta;

            return View();
        }




        public IActionResult VentasPorPeriodo()
        {
            var ventasPorDia = _context.Ventas
                .GroupBy(v => v.FechaVenta.Date) // Agrupa por la fecha (sin tiempo)
                .Select(g => new VentasPorDiaDto
                {
                    Fecha = g.Key, // Mantén la fecha como DateTime
                    Total = g.Sum(v => v.PrecioVenta * v.CantidadVendida)
                })
                .OrderBy(v => v.Fecha)
                .ToList();

            return View(ventasPorDia);
        }
        public IActionResult ProductosMasVendidos()
        {
            var productosMasVendidos = _context.Ventas
                .Include(v => v.Producto)
                .GroupBy(v => new { v.ProductoId, v.Producto.Nombre })
                .Select(g => new ProductoMasVendidoDto
                {
                    Nombre = g.Key.Nombre,
                    TotalVendido = g.Sum(v => v.CantidadVendida)
                })
                .OrderByDescending(p => p.TotalVendido)
                .Take(10)
                .ToList();

            return View(productosMasVendidos);
        }


        private async Task<WeatherData> GetWeatherDataAsync()
        {
            try
            {
                string apiKey = _configuration["OpenWeather:ApiKey"];
                string city = _configuration["OpenWeather:City"];
                string countryCode = _configuration["OpenWeather:CountryCode"];

                var url = $"https://api.openweathermap.org/data/2.5/weather?q={city},{countryCode}&appid={apiKey}&units=metric&lang=es";
                Console.WriteLine($"URL de la API: {url}"); // Depuración

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Respuesta de la API: {json}"); // Depuración
                    return JsonSerializer.Deserialize<WeatherData>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo datos del clima: {ex.Message}");
            }

            return null;
        }
        [HttpGet]
        public IActionResult Simulacion()
        {
            return View();
        }


        [HttpPost]
        public IActionResult Simulacion(int precioIncremento, int costoReduccion)
        {
            // Recuperar datos actuales
            var ventasTotales = _context.Ventas.Sum(v => (decimal?)v.PrecioVenta * v.CantidadVendida) ?? 0;
            var gananciasMensuales = _context.Ventas
                .Where(v => v.FechaVenta.Month == DateTime.Now.Month && v.FechaVenta.Year == DateTime.Now.Year)
                .Sum(v => (decimal?)v.PrecioVenta * v.CantidadVendida) ?? 0;

            var inventarioCostos = _context.Productos.Sum(p => p.Precio * p.Cantidad);

            // Aplicar simulación: Incremento de precios
            var nuevasVentasTotales = ventasTotales * (1 + (precioIncremento / 100m));
            var nuevasGananciasMensuales = gananciasMensuales * (1 + (precioIncremento / 100m));

            // Aplicar simulación: Reducción de costos
            var nuevosCostosInventario = inventarioCostos * (1 - (costoReduccion / 100m));

            // Calcular el impacto
            var impactoGanancias = nuevasGananciasMensuales - gananciasMensuales;
            var impactoCostos = inventarioCostos - nuevosCostosInventario;

            // Pasar resultados a la vista
            ViewBag.PrecioIncremento = precioIncremento;
            ViewBag.CostoReduccion = costoReduccion;
            ViewBag.NuevasVentasTotales = nuevasVentasTotales;
            ViewBag.NuevasGananciasMensuales = nuevasGananciasMensuales;
            ViewBag.NuevosCostosInventario = nuevosCostosInventario;
            ViewBag.ImpactoGanancias = impactoGanancias;
            ViewBag.ImpactoCostos = impactoCostos;

            return View();
        }
        private List<Producto> GetRecomendaciones(WeatherData weatherData)
        {
            var fechaActual = DateTime.Now;
            string estacionActual = ObtenerEstacion(fechaActual);
            List<Producto> productosRecomendados = new List<Producto>();

            try
            {
                var temperatura = weatherData?.Main?.Temp ?? 0; // Usa 0 como valor por defecto si no hay datos de clima
                Console.WriteLine($"Estación actual: {estacionActual}, Temperatura: {temperatura}");

                // Filtrar por estación y clima
                productosRecomendados = _context.Productos
                    .Include(p => p.Proveedor)
                    .Where(p =>
                        (estacionActual == "Primavera" && temperatura >= 10 && temperatura <= 25) || // Rango más amplio
                        (estacionActual == "Verano" && temperatura > 25) ||
                        (estacionActual == "Otoño" && temperatura <= 20 && temperatura > 5) ||
                        (estacionActual == "Invierno" && temperatura <= 5) ||
                        (estacionActual == "Primavera") // Añadir un filtro genérico de estación
                    )
                    .ToList();

                Console.WriteLine($"Productos recomendados encontrados: {productosRecomendados.Count}");

                // Si no hay productos, registra un mensaje
                if (!productosRecomendados.Any())
                {
                    Console.WriteLine("No se encontraron productos que coincidan con las condiciones de la temporada y el clima.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener recomendaciones: {ex.Message}");
            }

            return productosRecomendados;
        }
        [HttpGet]
        public IActionResult AnalisisRotacion()
        {
            // Datos de alta, media y baja rotación
            var altaRotacion = _context.Productos
                .Where(p => p.Cantidad > 50)
                .Select(p => new { p.Nombre, p.Cantidad })
                .ToList();

            var mediaRotacion = _context.Productos
                .Where(p => p.Cantidad > 20 && p.Cantidad <= 50)
                .Select(p => new { p.Nombre, p.Cantidad })
                .ToList();

            var bajaRotacion = _context.Productos
                .Where(p => p.Cantidad <= 20)
                .Select(p => new { p.Nombre, p.Cantidad })
                .ToList();

            // Serializar los datos para la vista
            ViewBag.AltaRotacion = JsonSerializer.Serialize(altaRotacion);
            ViewBag.MediaRotacion = JsonSerializer.Serialize(mediaRotacion);
            ViewBag.BajaRotacion = JsonSerializer.Serialize(bajaRotacion);

            return View();
        }

        private string ObtenerEstacion(DateTime fecha)
        {
            int mes = fecha.Month;

            if (mes >= 9 && mes <= 11) return "Primavera"; // Septiembre, Octubre, Noviembre
            if (mes == 12 || mes == 1 || mes == 2) return "Verano"; // Diciembre, Enero, Febrero
            if (mes >= 3 && mes <= 5) return "Otoño"; // Marzo, Abril, Mayo
            if (mes >= 6 && mes <= 8) return "Invierno"; // Junio, Julio, Agosto

            return "Desconocido";
        }

        // Método para mostrar las ganancias mensuales agrupadas por día
        public IActionResult GananciasMensuales()
        {
            var gananciasPorMes = _context.GananciasHistoricas
                .Where(g => g.FechaRegistro.Month == DateTime.Now.Month && g.FechaRegistro.Year == DateTime.Now.Year)
                .GroupBy(g => new { g.FechaRegistro.Year, g.FechaRegistro.Month, g.FechaRegistro.Day })
                .Select(g => new GananciaMensual
                {
                    Año = g.Key.Year,
                    Mes = g.Key.Month,
                    Dia = g.Key.Day,
                    TotalGanancias = g.Sum(h => h.TotalGanancia)
                })
                .ToList();

            ViewBag.GananciasPorMes = gananciasPorMes;

            // Calcular el total de ganancias del mes
            decimal totalGananciasMes = gananciasPorMes.Sum(g => g.TotalGanancias);

            // Definir la meta de ganancias mensuales
            decimal metaGananciasMensuales = 3000m; // Ajusta esta meta según tus objetivos

            // Calcular el porcentaje de avance en base a la meta
            decimal porcentajeGananciasMensuales = (totalGananciasMes / metaGananciasMensuales) * 100;

            ViewBag.TotalGananciasMes = totalGananciasMes;
            ViewBag.MetaGananciasMensuales = metaGananciasMensuales;
            ViewBag.PorcentajeGananciasMensuales = porcentajeGananciasMensuales;

            return View();
        }
        // Método para exportar a PDF
        public IActionResult ExportarPDF()
        {
            var gananciasPorMes = _context.Ventas
                .GroupBy(v => new { v.FechaVenta.Year, v.FechaVenta.Month, v.FechaVenta.Day })
                .Select(g => new GananciaMensual
                {
                    Año = g.Key.Year,
                    Mes = g.Key.Month,
                    Dia = g.Key.Day,
                    TotalGanancias = g.Sum(v => v.PrecioVenta * v.CantidadVendida)
                }).ToList();

            using (var stream = new MemoryStream())
            {
                Document pdfDoc = new Document();
                PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();
                pdfDoc.Add(new Paragraph("Reporte de Ganancias Mensuales"));

                PdfPTable table = new PdfPTable(4);
                table.AddCell("Año");
                table.AddCell("Mes");
                table.AddCell("Día");
                table.AddCell("Total Ganancias");

                foreach (var item in gananciasPorMes)
                {
                    table.AddCell(item.Año.ToString());
                    table.AddCell(item.Mes.ToString());
                    table.AddCell(item.Dia.ToString());
                    table.AddCell(item.TotalGanancias.ToString("C0", new System.Globalization.CultureInfo("es-CL")));
                }

                pdfDoc.Add(table);
                pdfDoc.Close();
                return File(stream.ToArray(), "application/pdf", "GananciasMensuales.pdf");
            }
        }
        public IActionResult GenerarCotizacion()
        {
            var productos = _context.Productos.ToList(); // Recuperar todos los productos
            ViewBag.Productos = productos; // Pasar los productos disponibles al ViewBag
            return View();
        }

        [HttpPost]
        public IActionResult GenerarCotizacion(int[] productoIds, int[] cantidades)
        {
            if (productoIds == null || cantidades == null || productoIds.Length != cantidades.Length)
            {
                ViewBag.Error = "Selecciona los productos e ingresa las cantidades correctamente.";
                return RedirectToAction("GenerarCotizacion");
            }

            var detalles = new List<DetalleCotizacion>();

            for (int i = 0; i < productoIds.Length; i++)
            {
                var producto = _context.Productos.FirstOrDefault(p => p.ProductoId == productoIds[i]);
                if (producto != null)
                {
                    var cantidad = cantidades[i];
                    var precioTotal = cantidad * producto.Precio;

                    detalles.Add(new DetalleCotizacion
                    {
                        Nombre = producto.Nombre,
                        Cantidad = cantidad,
                        PrecioUnitario = producto.Precio,
                        PrecioTotal = precioTotal
                    });
                }
            }

            // Calcular totales
            ViewBag.Cotizacion = detalles;
            ViewBag.TotalNeto = detalles.Sum(d => d.PrecioTotal);
            ViewBag.Iva = ViewBag.TotalNeto * 0.19m; // IVA del 19%
            ViewBag.TotalConIva = ViewBag.TotalNeto + ViewBag.Iva;

            return View("ResultadoCotizacion");
        }

        // Método para exportar a Excel
        public IActionResult ExportarExcel()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var gananciasPorMes = _context.Ventas
                .GroupBy(v => new { v.FechaVenta.Year, v.FechaVenta.Month, v.FechaVenta.Day })
                .Select(g => new GananciaMensual
                {
                    Año = g.Key.Year,
                    Mes = g.Key.Month,
                    Dia = g.Key.Day,
                    TotalGanancias = g.Sum(v => v.PrecioVenta * v.CantidadVendida)
                }).ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Ganancias Mensuales");
                worksheet.Cells[1, 1].Value = "Año";
                worksheet.Cells[1, 2].Value = "Mes";
                worksheet.Cells[1, 3].Value = "Día";
                worksheet.Cells[1, 4].Value = "Total Ganancias";

                for (int i = 0; i < gananciasPorMes.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = gananciasPorMes[i].Año;
                    worksheet.Cells[i + 2, 2].Value = gananciasPorMes[i].Mes;
                    worksheet.Cells[i + 2, 3].Value = gananciasPorMes[i].Dia;
                    worksheet.Cells[i + 2, 4].Value = gananciasPorMes[i].TotalGanancias;
                }

                return File(package.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "GananciasMensuales.xlsx");
            }
        }

        // Método para mostrar ganancias por planta
        public IActionResult Ganancias()
        {
            // Consultar ganancias por producto desde la tabla Ventas
            var gananciasPorPlanta = _context.Ventas
                .Include(v => v.Producto)
                .GroupBy(v => new { v.ProductoId, v.Producto.Nombre })
                .Select(g => new
                {
                    ProductoId = g.Key.ProductoId,
                    NombreProducto = g.Key.Nombre,
                    Ganancias = g.Sum(v => v.PrecioVenta * v.CantidadVendida),
                    CantidadVendida = g.Sum(v => v.CantidadVendida)
                })
                .ToList();

            // Calcular las ganancias totales mensuales desde el historial de ganancias
            var gananciasMensuales = _context.GananciasHistoricas
                .Where(g => g.FechaRegistro.Month == DateTime.Now.Month && g.FechaRegistro.Year == DateTime.Now.Year)
                .Sum(g => (decimal?)g.TotalGanancia) ?? 0;

            ViewBag.GananciasPorPlanta = gananciasPorPlanta;
            ViewBag.GananciasMensuales = gananciasMensuales;

            return View();
        }


    }
}


