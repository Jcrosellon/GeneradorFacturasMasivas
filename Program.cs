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
                                // Las propiedades eliminadas no se utilizan
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

        private static void GenerarPDF(List<Factura> facturas, string carpetaFacturas)
        {
            if (facturas.Count == 0)
                return;

            string? cliente = facturas[0].Cliente;
            string? numeroFactura = facturas[0].NumeroFactura;
            string? nombreArchivoPDF = Path.Combine(
                carpetaFacturas,
                $"factura_{cliente}_{numeroFactura}.pdf"
            );

            PdfDocument document = new PdfDocument();
            document.Info.Title = "Factura";

            // Fuentes
            XFont fontRegular = new XFont("Arial", 10, XFontStyle.Regular);
            XFont fontBold = new XFont("Arial", 10, XFontStyle.Bold);
            int rowHeight = 20;

            // Cargar y dibujar el logo
            XImage logo = XImage.FromFile("Logo/Logo.png");

            // Configuración de dimensiones y posiciones para las tablas
            int leftTableX = 20;
            int rightTableX = 250; // Ajustado para tener margen
            int tableWidth = 250; // Ajustado para tener margen
            int startY = 100; // Margen superior
            int marginBottom = 30; // Margen inferior
            int yProductos = startY; // Para la posición de la tabla de productos

            // Agregar la primera página
            PdfPage page = document.AddPage();
            page.Size = PdfSharpCore.PageSize.Letter; // Establecer tamaño carta
            page.TrimMargins.All = 20; // Márgenes de 20 puntos en todos los lados
            XGraphics gfx = XGraphics.FromPdfPage(page);

            // Dibuja el logo
            gfx.DrawImage(logo, 20, 20, 150, 75);

            // Función para dibujar la información en la página
            void DibujarInformacion()
            {
                // Tabla izquierda - Información de la empresa
                gfx.DrawRectangle(XPens.Black, leftTableX, startY, tableWidth, rowHeight);
                gfx.DrawString(
                    "LOGISTICA FERRETERA S.A.S. - NIT 900,236,553-1",
                    fontBold,
                    XBrushes.Black,
                    new XRect(leftTableX, startY, tableWidth, rowHeight),
                    XStringFormats.Center
                );

                string[,] infoEmpresa =
                {
                    { "Dirección", "CEDI Paloquemao : Carrera 22 # 19-95" },
                    { "Vendedor", "CARLOS ARIAS" },
                    { "Telefonos", "(+57) 300 912 7030" },
                };

                for (int i = 0; i < infoEmpresa.GetLength(0); i++)
                {
                    int yPos = startY + rowHeight + (i * rowHeight);
                    gfx.DrawRectangle(XPens.Black, leftTableX, yPos, tableWidth, rowHeight);

                    // Columna de etiqueta
                    gfx.DrawRectangle(XPens.Black, leftTableX, yPos, 60, rowHeight);
                    gfx.DrawString(
                        infoEmpresa[i, 0],
                        fontBold,
                        XBrushes.Black,
                        new XRect(leftTableX, yPos, 60, rowHeight),
                        XStringFormats.Center
                    );

                    // Columna de valor
                    gfx.DrawString(
                        infoEmpresa[i, 1],
                        fontRegular,
                        XBrushes.Black,
                        new XRect(leftTableX + 65, yPos, tableWidth - 65, rowHeight),
                        XStringFormats.CenterLeft
                    );
                }

                // Tabla derecha
                gfx.DrawRectangle(XPens.Black, rightTableX, startY, tableWidth, rowHeight);
                gfx.DrawString(
                    "REGIMEN COMUN",
                    fontBold,
                    XBrushes.Black,
                    new XRect(rightTableX, startY, tableWidth, rowHeight),
                    XStringFormats.Center
                );

                // ANEXO FACT.ELECTR.No y FECHA
                string[,] infoFactura =
                {
                    { "ANEXO FACT.ELECTR.No:", $"FE    {numeroFactura}" },
                    { "FECHA :", facturas[0].Fecha.ToString("MMM/dd/yyyy") },
                };

                for (int i = 0; i < infoFactura.GetLength(0); i++)
                {
                    int yPos = startY + rowHeight + (i * rowHeight);
                    gfx.DrawRectangle(XPens.Black, rightTableX, yPos, tableWidth, rowHeight);

                    gfx.DrawString(
                        infoFactura[i, 0],
                        fontBold,
                        XBrushes.Black,
                        new XRect(rightTableX, yPos, tableWidth, rowHeight),
                        XStringFormats.Center
                    );

                    gfx.DrawString(
                        infoFactura[i, 1],
                        fontRegular,
                        XBrushes.Black,
                        new XRect(rightTableX + tableWidth / 2, yPos, tableWidth / 2, rowHeight),
                        XStringFormats.Center
                    );
                }

                // Mostrar datos del cliente
                int clienteHeaderY = startY + rowHeight * (infoEmpresa.GetLength(0) + 1);
                string?[,] datosCliente =
                {
                    { "Razón Social:", facturas[0].Cliente },
                };

                for (int i = 0; i < datosCliente.GetLength(0); i++)
                {
                    int yPos = clienteHeaderY + rowHeight + (i * rowHeight);
                    gfx.DrawRectangle(XPens.Black, rightTableX, yPos, tableWidth, rowHeight);
                    gfx.DrawString(
                        datosCliente[i, 0],
                        fontBold,
                        XBrushes.Black,
                        new XRect(rightTableX + 5, yPos, tableWidth / 2, rowHeight),
                        XStringFormats.CenterLeft
                    );

                    gfx.DrawString(
                        datosCliente[i, 1],
                        fontRegular,
                        XBrushes.Black,
                        new XRect(rightTableX + tableWidth / 2, yPos, tableWidth / 2, rowHeight),
                        XStringFormats.Center
                    );
                }
            }

            // Dibujar información del cliente solo en la primera página
            DibujarInformacion();

            // Producto
            int yProductosHeader = startY + rowHeight * (facturas[0].Cliente != null ? 4 : 3); // Ajusta según el número de filas
            gfx.DrawRectangle(
                XPens.Black,
                leftTableX,
                yProductosHeader,
                tableWidth * 2 + 5,
                rowHeight
            );
            gfx.DrawString(
                "PRODUCTOS",
                fontBold,
                XBrushes.Black,
                new XRect(leftTableX, yProductosHeader, tableWidth * 2 + 5, rowHeight),
                XStringFormats.Center
            );

            yProductos = yProductosHeader + rowHeight;

            // Dibujar los productos
            foreach (var factura in facturas)
            {
                foreach (var producto in factura.Productos)
                {
                    // Verificar si es necesario agregar una nueva página
                    if (yProductos + rowHeight > page.Height - marginBottom) // Se puede ajustar según el espacio disponible
                    {
                        page = document.AddPage();
                        page.Size = PdfSharpCore.PageSize.Letter;
                        page.TrimMargins.All = 20;
                        gfx = XGraphics.FromPdfPage(page);
                        yProductos = startY; // Reiniciar posición para la nueva página

                        // En las páginas siguientes, solo dibujar los productos
                        int yProductosHeaderNextPage = startY; // Ajusta la posición para la nueva página
                        gfx.DrawRectangle(
                            XPens.Black,
                            leftTableX,
                            yProductosHeaderNextPage,
                            tableWidth * 2 + 5,
                            rowHeight
                        );
                        gfx.DrawString(
                            "PRODUCTOS",
                            fontBold,
                            XBrushes.Black,
                            new XRect(
                                leftTableX,
                                yProductosHeaderNextPage,
                                tableWidth * 2 + 5,
                                rowHeight
                            ),
                            XStringFormats.Center
                        );

                        yProductos = yProductosHeaderNextPage + rowHeight; // Actualiza la posición de los productos
                    }

                    gfx.DrawRectangle(
                        XPens.Black,
                        leftTableX,
                        yProductos,
                        tableWidth * 2 + 5,
                        rowHeight
                    );
                    gfx.DrawString(
                        producto.Descripcion,
                        fontRegular,
                        XBrushes.Black,
                        new XRect(leftTableX, yProductos, tableWidth, rowHeight),
                        XStringFormats.CenterLeft
                    );

                    gfx.DrawString(
                        producto.Cantidad.ToString(),
                        fontRegular,
                        XBrushes.Black,
                        new XRect(leftTableX + tableWidth, yProductos, tableWidth / 4, rowHeight),
                        XStringFormats.Center
                    );

                    gfx.DrawString(
                        $"{producto.VrUnitario:C}",
                        fontRegular,
                        XBrushes.Black,
                        new XRect(
                            leftTableX + tableWidth + tableWidth / 4,
                            yProductos,
                            tableWidth / 4,
                            rowHeight
                        ),
                        XStringFormats.Center
                    );

                    gfx.DrawString(
                        $"{producto.VrTotal:C}",
                        fontRegular,
                        XBrushes.Black,
                        new XRect(
                            leftTableX + tableWidth * 2,
                            yProductos,
                            tableWidth / 4,
                            rowHeight
                        ),
                        XStringFormats.Center
                    );

                    yProductos += rowHeight;
                }
            }

            // Guardar el documento
            document.Save(nombreArchivoPDF);
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
