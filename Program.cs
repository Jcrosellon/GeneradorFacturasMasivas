using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RdlcRenderer;

namespace GeneradorFacturasMasivas
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string carpetaFacturas = "Facturas";

            if (!Directory.Exists(carpetaFacturas))
            {
                Directory.CreateDirectory(carpetaFacturas);
                Console.WriteLine($"Carpeta '{carpetaFacturas}' creada.");
            }

            var listaFacturas = ObtenerFacturasDesdeBD();

            if (listaFacturas.Count > 0)
            {
                Console.WriteLine($"{listaFacturas.Count} facturas encontradas. Generando PDFs...");

                var facturasAgrupadas = listaFacturas
                    .GroupBy(f => new { f.Cliente, f.NumeroFactura })
                    .Select(g => g.ToList())
                    .ToList();

                foreach (var grupo in facturasAgrupadas)
                {
                    Console.WriteLine(
                        $"Generando PDF para Cliente = {grupo[0].Cliente}, NumeroFactura = {grupo[0].NumeroFactura}"
                    );
                    await GenerarPDFConRDLC(grupo, carpetaFacturas);
                }

                Console.WriteLine("Todas las facturas se generaron exitosamente.");
            }
            else
            {
                Console.WriteLine("No se encontraron facturas para procesar.");
            }
        }

        private static List<Factura> ObtenerFacturasDesdeBD()
        {
            List<Factura> facturas = new List<Factura>();
            string connectionString =
                "Server=localhost;Database=TEST;User Id=sa;Password=pazJc2601;";
            string query =
                "SELECT Cliente, NumeroFactura, Fecha, Producto, Cantidad, TotalVenta FROM MasivosPDF";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        string cliente = reader["Cliente"]?.ToString() ?? string.Empty;
                        string numeroFactura = reader["NumeroFactura"]?.ToString() ?? string.Empty;

                        var factura = facturas.FirstOrDefault(f =>
                            f.Cliente == cliente && f.NumeroFactura == numeroFactura
                        );
                        if (factura == null)
                        {
                            factura = new Factura
                            {
                                Cliente = cliente,
                                NumeroFactura = numeroFactura,
                                Fecha = reader["Fecha"] as DateTime? ?? DateTime.Now,
                            };
                            facturas.Add(factura);
                        }

                        Producto producto = new Producto
                        {
                            Cantidad = Convert.ToInt32(reader["Cantidad"]),
                            Descripcion = reader["Producto"]?.ToString() ?? string.Empty,
                            VrUnitario =
                                Convert.ToDecimal(reader["TotalVenta"])
                                / Convert.ToInt32(reader["Cantidad"]),
                            VrTotal = Convert.ToDecimal(reader["TotalVenta"]),
                            Iva = 0.0m,
                        };
                        factura.Productos.Add(producto);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al acceder a la base de datos: {ex.Message}");
                }
            }
            return facturas;
        }

        private static async Task GenerarPDFConRDLC(List<Factura> facturas, string carpetaFacturas)
        {
            if (facturas == null || facturas.Count == 0)
            {
                Console.WriteLine("No se puede generar PDF. Lista vacía.");
                return;
            }

            string cliente = facturas[0]?.Cliente.Replace(" ", "_") ?? "ClienteDesconocido";
            string numeroFactura = facturas[0]?.NumeroFactura ?? "FacturaDesconocida";
            string nombreArchivoPDF = Path.Combine(
                carpetaFacturas,
                $"factura_{cliente}_{numeroFactura}.pdf"
            );

            string pathReport = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Reports",
                "FacturaReport.rdlc"
            );

            // Crear renderer
            var renderer = new ReportRenderer();
            // Cargar el reporte
            renderer.LoadReport(pathReport);

            // Preparar el DataSource
            var dataSource = facturas
                .SelectMany(f =>
                    f.Productos.Select(p => new
                    {
                        f.Cliente,
                        f.NumeroFactura,
                        f.Fecha,
                        Producto = p.Descripcion,
                        p.Cantidad,
                        p.VrUnitario,
                        p.VrTotal,
                    })
                )
                .ToList();

            renderer.SetDataSource("FacturaDataSet", dataSource);

            // Renderizar a PDF
            var bytes = renderer.Render("PDF");

            await File.WriteAllBytesAsync(nombreArchivoPDF, bytes);
            Console.WriteLine($"Factura generada y guardada en {nombreArchivoPDF}");
        }
    }

    public class Factura
    {
        public string? Cliente { get; set; }
        public string? NumeroFactura { get; set; }
        public DateTime Fecha { get; set; }
        public List<Producto> Productos { get; set; } = new List<Producto>();
    }

    public class Producto
    {
        public int Cantidad { get; set; }
        public string? Descripcion { get; set; }
        public decimal Iva { get; set; }
        public decimal VrUnitario { get; set; }
        public decimal VrTotal { get; set; }
    }
}
