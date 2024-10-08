using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace GeneradorFacturasMasivas
{
    class Program
    {
        static void Main(string[] args)
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

                foreach (var grupo in facturasAgrupadas)
                {
                    GenerarPDF(grupo, carpetaFacturas); // Pasar la carpeta como parámetro
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
                "Server=192.168.0.119;Database=TEST;User Id=sa;Password=HpMl110g7*;";

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
                        Factura factura = new Factura
                        {
                            Cliente = reader["Cliente"].ToString() ?? string.Empty,
                            NumeroFactura = reader["NumeroFactura"].ToString() ?? string.Empty,
                            Fecha = reader["Fecha"] as DateTime? ?? DateTime.Now,
                            Producto = reader["Producto"].ToString() ?? string.Empty,
                            Cantidad = Convert.ToInt32(reader["Cantidad"]),
                            TotalVenta = Convert.ToDecimal(reader["TotalVenta"]),
                        };
                        facturas.Add(factura);
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

        private static void GenerarPDF(List<Factura> facturas, string carpetaFacturas)
        {
            if (facturas.Count == 0)
                return;

            string cliente = facturas[0].Cliente;
            string numeroFactura = facturas[0].NumeroFactura;
            string nombreArchivoPDF = Path.Combine(
                carpetaFacturas,
                $"factura_{cliente}_{numeroFactura}.pdf"
            );

            PdfDocument document = new PdfDocument();
            document.Info.Title = "Factura";

            PdfPage page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);

            // Fuentes
            XFont fontRegular = new XFont("Arial", 10, XFontStyle.Regular);
            XFont fontBold = new XFont("Arial", 10, XFontStyle.Bold);

            // Cargar y dibujar el logo
            XImage logo = XImage.FromFile("Logo/Logo.png");
            gfx.DrawImage(logo, 20, 20, 150, 75);

            // Rectángulo superior derecho - REGIMEN COMUN y datos de factura
            int rightBoxWidth = 150;
            int rightBoxX = (int)(page.Width - rightBoxWidth - 20);

            // Marco exterior
            gfx.DrawRectangle(XPens.Black, rightBoxX, 20, rightBoxWidth, 120);

            // REGIMEN COMUN
            gfx.DrawRectangle(XPens.Black, rightBoxX, 20, rightBoxWidth, 25);
            gfx.DrawString(
                "REGIMEN COMUN",
                fontBold,
                XBrushes.Black,
                new XRect(rightBoxX, 20, rightBoxWidth, 25),
                XStringFormats.Center
            );

            // ANEXO FACT.ELECTR.No
            int yPos = 45;
            gfx.DrawLine(XPens.Black, rightBoxX + 200, yPos, rightBoxX + rightBoxWidth, yPos);
            gfx.DrawString(
                "ANEXO FACT.ELECTR.No:",
                fontBold,
                XBrushes.Black,
                rightBoxX + 5,
                yPos - 5
            );
            gfx.DrawString(
                $"FE    {numeroFactura}",
                fontBold,
                XBrushes.Black,
                rightBoxX + 205,
                yPos - 5
            );

            // FECHA
            yPos += 25;
            gfx.DrawLine(XPens.Black, rightBoxX + 200, yPos, rightBoxX + rightBoxWidth, yPos);
            gfx.DrawString("FECHA :", fontBold, XBrushes.Black, rightBoxX + 5, yPos - 5);
            gfx.DrawString(
                facturas[0].Fecha.ToString("MMM/dd/yyyy"),
                fontRegular,
                XBrushes.Black,
                rightBoxX + 205,
                yPos - 5
            );

            // DATOS DEL CLIENTE
            yPos += 25;
            gfx.DrawRectangle(XPens.Black, rightBoxX, yPos, rightBoxWidth, 25);
            gfx.DrawString(
                "DATOS DEL CLIENTE",
                fontBold,
                XBrushes.Black,
                new XRect(rightBoxX, yPos, rightBoxWidth, 25),
                XStringFormats.Center
            );

            // Información de la empresa - Tabla izquierda
            int leftBoxX = 20;
            int leftBoxWidth = (int)(page.Width - rightBoxWidth - 120);
            gfx.DrawRectangle(XPens.Black, leftBoxX, 100, leftBoxWidth, 90);

            gfx.DrawString(
                "LOGISTICA FERRETERA S.A.S. - NIT 900,236,553-1",
                fontBold,
                XBrushes.Black,
                new XRect(leftBoxX + 5, 105, leftBoxWidth - 10, 20),
                XStringFormats.TopLeft
            );

            string[,] infoEmpresa =
            {
                { "Dirección", "CEDI Paloquemao : Carrera 22 # 19-95" },
                { "Vendedor", "CARLOS ARIAS" },
                { "Telefonos", "(+57) 300 912 7030" },
            };

            for (int i = 0; i < infoEmpresa.GetLength(0); i++)
            {
                int yPosInfo = 125 + (i * 20);
                gfx.DrawString(infoEmpresa[i, 0], fontBold, XBrushes.Black, leftBoxX + 5, yPosInfo);
                gfx.DrawString(
                    infoEmpresa[i, 1],
                    fontRegular,
                    XBrushes.Black,
                    leftBoxX + 70,
                    yPosInfo
                );
            }

            // Tabla de productos
            int yProductos = 220;
            string[] encabezados = { "CANT.", "DESCRIPCION", "% IVA", "VR. UNIT.", "VR. TOTAL" };
            int[] anchos = { 60, (int)(page.Width - 280), 60, 70, 70 };
            int xInicio = 20;

            // Dibujar encabezados
            for (int i = 0; i < encabezados.Length; i++)
            {
                gfx.DrawRectangle(XPens.Black, xInicio, yProductos, anchos[i], 20);
                gfx.DrawString(
                    encabezados[i],
                    fontBold,
                    XBrushes.Black,
                    new XRect(xInicio + 2, yProductos, anchos[i] - 4, 20),
                    i == 1 ? XStringFormats.Center : XStringFormats.Center
                );
                xInicio += anchos[i];
            }

            // Dibujar filas de productos
            yProductos += 20;
            foreach (var factura in facturas)
            {
                xInicio = 20;

                // Dibujar las celdas y el contenido
                for (int i = 0; i < encabezados.Length; i++)
                {
                    gfx.DrawRectangle(XPens.Black, xInicio, yProductos, anchos[i], 20);

                    string valor = i switch
                    {
                        0 => factura.Cantidad.ToString(),
                        1 => factura.Producto,
                        2 => "19",
                        3 => factura.TotalVenta.ToString("N0"),
                        4 => (factura.Cantidad * factura.TotalVenta).ToString("N0"),
                        _ => "",
                    };

                    XStringFormat formato =
                        i == 1 ? XStringFormats.CenterLeft : XStringFormats.Center;
                    gfx.DrawString(
                        valor,
                        fontRegular,
                        XBrushes.Black,
                        new XRect(xInicio + 2, yProductos, anchos[i] - 4, 20),
                        formato
                    );

                    xInicio += anchos[i];
                }
                yProductos += 20;
            }

            document.Save(nombreArchivoPDF);
            Console.WriteLine($"PDF generado: {nombreArchivoPDF}");
        }

        public class Factura
        {
            public string Cliente { get; set; } = string.Empty;
            public string NumeroFactura { get; set; } = string.Empty;
            public DateTime Fecha { get; set; } = DateTime.Now;
            public string Producto { get; set; } = string.Empty;
            public int Cantidad { get; set; }
            public decimal TotalVenta { get; set; }

            // Nuevos campos para coincidir con la imagen
            public string NIT { get; set; } = string.Empty;
            public string Direccion { get; set; } = string.Empty;
            public string Telefono { get; set; } = string.Empty;
            public string CiudadCliente { get; set; } = string.Empty;
        }
    }
}
