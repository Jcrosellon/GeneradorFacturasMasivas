using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GeneradorFacturasMasivas
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string carpetaFacturas = "Facturas"; // Nombre de la carpeta para guardar las facturas

            // Verificar si la carpeta existe, si no, crearla
            if (!Directory.Exists(carpetaFacturas))
            {
                Directory.CreateDirectory(carpetaFacturas);
                Console.WriteLine($"Carpeta '{carpetaFacturas}' creada.");
            }

            var listaFacturas = ObtenerFacturasDesdeBD();

            if (listaFacturas.Count > 0)
            {
                Console.WriteLine($"{listaFacturas.Count} facturas encontradas. Generando PDFs...");

                // Agrupar las facturas por cliente y número de factura
                var facturasAgrupadas = listaFacturas
    .GroupBy(f => new { f.Cliente, f.NumeroFactura })
    .Select(g => g.ToList())
    .ToList();

// Asegúrate de que 'facturasAgrupadas' contenga los datos esperados
foreach (var grupo in facturasAgrupadas)
{
    Console.WriteLine($"Grupo: Cliente = {grupo[0].Cliente}, NumeroFactura = {grupo[0].NumeroFactura}, Productos = {grupo.Count}");
    await GenerarPDFDesdeReportServer(grupo, carpetaFacturas);
}


                Console.WriteLine("Facturas generadas exitosamente.");
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
                "Server=192.168.0.119;Database=TEST;User Id=sa;Password=HpMl110g7*;"; // Reemplaza por tu conexión

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

                        // Busca la factura en la lista existente
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

                        // Crear un nuevo producto y agregarlo a la factura
                        Producto producto = new Producto
                        {
                            Cantidad = Convert.ToInt32(reader["Cantidad"]),
                            Descripcion = reader["Producto"]?.ToString() ?? string.Empty,
                            Iva = 0.0m, // Ajusta esto según tu lógica para calcular el IVA
                            VrUnitario =
                                Convert.ToDecimal(reader["TotalVenta"])
                                / Convert.ToInt32(reader["Cantidad"]),
                            VrTotal = Convert.ToDecimal(reader["TotalVenta"]),
                        };
                        factura.Productos.Add(producto);
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al acceder a la base de datos: " + ex.Message);
                }
            }

            return facturas;
        }

        private static async Task GenerarPDFDesdeReportServer(List<Factura> facturas, string carpetaFacturas)
        {
            if (facturas.Count == 0)
                return;

            string? cliente = facturas[0].Cliente;
            string? numeroFactura = facturas[0].NumeroFactura;
            string nombreArchivoPDF = Path.Combine(carpetaFacturas, $"factura_{cliente}_{numeroFactura}.pdf");

            // Ajusta la URL del servidor de ReportServer
            string reportServerUrl = "http://localhost:8080/ReportServer/api/report"; // Cambia a la URL de tu ReportServer
 // Cambia a la URL de tu ReportServer
            string reportName = "Factura"; // Nombre del reporte en ReportServer
            string reportFormat = "pdf"; // Formato deseado

            using (HttpClient client = new HttpClient())
            {
                // Crear la URL del reporte con parámetros
                var reportUrl = $"{reportServerUrl}/{reportName}.{reportFormat}?cliente={cliente}&numeroFactura={numeroFactura}";

                try
                {
                    // Obtener el reporte en formato PDF
                    HttpResponseMessage response = await client.GetAsync(reportUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        byte[] reportData = await response.Content.ReadAsByteArrayAsync();
                        // Guardar el archivo PDF
                        await File.WriteAllBytesAsync(nombreArchivoPDF, reportData);
                        Console.WriteLine($"Factura {numeroFactura} generada y guardada.");
                    }
                    else
                    {
                        Console.WriteLine($"Error al generar la factura {numeroFactura}: {response.ReasonPhrase}");
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    Console.WriteLine($"Error de conexión al servidor de ReportServer: {httpEx.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error inesperado: {ex.Message}");
                }
            }
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
