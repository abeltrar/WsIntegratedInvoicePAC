using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using WsIntegratedInvoicePAC.Controller;
using WsIntegratedInvoicePAC.Models;

namespace WsIntegratedInvoicePAC.ViewPDF
{

    public class FacturaPDF : IDocument
    {
        private readonly byte[] _logo;
        private readonly FE_Invoice_Transaccion _encabezado;
        private readonly List<ItemFactura> _items;
        private readonly string _dateSentToDgi;
        private readonly string _signatureFirst6;
        private readonly string _fechaVencimiento;
        private readonly string _qrB64;

        public FacturaPDF(FacturaData data)
        {
            _logo = data.Logo;
            _encabezado = data.Encabezado;
            _items = data.Items.ToList();
            _dateSentToDgi = data.DateSentToDgi;
            _signatureFirst6 = data.SignatureFirst6;
            _fechaVencimiento = data.FechaVencimiento;
            _qrB64 = data.QrB64;

           

        }



        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontFamily("Open Sans").FontSize(8));

                // ===== ENCABEZADO =====
                page.Header().Column(headerCol =>
                {
                    headerCol.Item().Element(ComposeHeader);
                    headerCol.Item().Element(ComposeBillToShipTo);
                    headerCol.Item().Element(ComposeInfoBlock);
                });

                // ===== CONTENIDO PRINCIPAL (SE PUEDE DIVIDIR) =====
                page.Content()
                    .Column(col =>
                    {
                        col.Item().Element(ComposeContent);
                    });

                // ===== FOOTER FIJO =====
                page.Footer()
                    .Element(ComposeFooter);
            });
        }

        void ComposeHeader(IContainer container)
        {


            container.Row(row =>
            {
                // Sección 1: Logo y Dirección 
                row.RelativeColumn(1.5f).Row(headerLeftRow =>
                {
                    if (_logo != null && _logo.Length > 0)
                        headerLeftRow.ConstantColumn(80).Image(_logo);
                    else
                        headerLeftRow.ConstantColumn(80).Height(60).Text("B'LEATHER").Bold().FontSize(16);

                    headerLeftRow.ConstantColumn(10);

                    headerLeftRow.RelativeColumn().Column(textCol =>
                    {
                        textCol.Item().Height(5);
                        textCol.Item().Text("B'LEATHER MANUFACTURING, INC.").Bold().FontSize(10);
                        textCol.Item().Text("CITIBANK, N.A.");
                        textCol.Item().Text("111 WALL STREET");
                        textCol.Item().Text("NEW YORK, NY 10043, USA.");
                        textCol.Item().Text("ABA:" + _encabezado.ABA);
                        textCol.Item().Text("SWIFT:" + _encabezado.SWIFT);
                        textCol.Item().Text("ACCT: B'LEATHER MANUFACTURING, INC.");
                        textCol.Item().Text("ACCT #: " + _encabezado.ACCT);
                    });
                });

              
                //ENCABEZADO INFO DE FACTURA
                row.RelativeColumn(1).Column(col =>
                {
                    // Bloque de INVOICE 
                    col.Item().AlignRight().Column(invoiceBlock =>
                    {
                        string nombre_factura = "";

                        if (_encabezado.Tipo_Factura == "CU")
                        {
                            nombre_factura = "Electronic Consumer Invoice";
                        }
                        else
                        {
                            nombre_factura = "Electronic Credit Note";

                        }
                        invoiceBlock.Item().AlignCenter().Text(nombre_factura).Bold().FontSize(14);
                        invoiceBlock.Item().PaddingBottom(2);
                        invoiceBlock.Item().Border(1).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(); columns.RelativeColumn(); columns.RelativeColumn();
                            });
                            table.Cell().BorderBottom(1).BorderRight(1).Background(Colors.Grey.Lighten2).Padding(2).Text("Invoice #").Bold();
                            table.Cell().BorderBottom(1).BorderRight(1).Background(Colors.Grey.Lighten2).Padding(2).Text("Invoice Date").Bold();
                            table.Cell().BorderBottom(1).Background(Colors.Grey.Lighten2).Padding(2).Text("Page").Bold();
                            table.Cell().BorderRight(1).Padding(2).Text(_encabezado.Factura_Numero);
                            table.Cell().BorderRight(1).Padding(2).Text(_encabezado.Factura_Fecha_Emision.ToString("dd/MM/yyyy"));
                            table.Cell().Padding(2).Text(x =>
                            {
                              
                                x.CurrentPageNumber();
                                x.Span(" of ");
                                x.TotalPages();
                            });
                        });
                    });

                    col.Item()
                        .PaddingBottom(15)
                        .PaddingTop(15)
                        .Column(innerCol =>
                        {
                            innerCol.Spacing(0); // sin espacio entre líneas
                            innerCol.Item().Text("NCF #" + _encabezado.NCF).Bold();
                            if (_encabezado.Tipo_Factura == "CU")
                            {
                                innerCol.Item().Text("Due Date: " + _fechaVencimiento).Bold();
                            }
                            if (_encabezado.Tipo_Factura == "CR")
                            {
                                innerCol.Item().Text("NCF Modified #" + _encabezado.Factura_Afectada_NC).Bold();
                                innerCol.Item().Text("Cancels the modified NCF").Bold().FontSize(10);
                            }
                        });

                });
            });
        }


        void ComposeContent(IContainer container)
        {
            container.Element(ComposeDetailsTable);
        }


        void ComposeBillToShipTo(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeColumn().Padding(5).Column(c =>
                {
                    c.Item().Text("Bill To:").Bold();
                    c.Item().Text(_encabezado.Cliente_Nombre);
                    c.Item().Text(_encabezado.Cliente_Direccion1);
                    c.Item().Text(_encabezado.Cliente_Direccion2);
                    c.Item().Text("RNC: " + _encabezado.Cliente_Documento_Identidad);
                });
                row.ConstantColumn(10);
                row.RelativeColumn().Padding(5).Column(c =>
                {
                    c.Item().Text("Ship To:").Bold();
                    c.Item().Text(_encabezado.Cliente_Nombre);
                    c.Item().Text(_encabezado.Direccion_Envio1);
                    c.Item().Text(_encabezado.Direccion_Envio2);
                    c.Item().Text("RNC: " + _encabezado.Cliente_Documento_Identidad);
                });
            });
        }




        // MÉTODO PARA LA PRIMERA TABLA DE INFORMACIÓN
        void ComposeInfoBar1(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(); columns.RelativeColumn();
                    columns.RelativeColumn(); columns.RelativeColumn();
                });

                table.Cell().BorderBottom(1).BorderRight(1).Background(Colors.Grey.Lighten2).Padding(3).Text("CUSTOMER PO NUMBER").Bold();
                table.Cell().BorderBottom(1).BorderRight(1).Background(Colors.Grey.Lighten2).Padding(3).Text("TERMS").Bold();
                table.Cell().BorderBottom(1).BorderRight(1).Background(Colors.Grey.Lighten2).Padding(3).Text("SHIP VIA").Bold();
                table.Cell().BorderBottom(1).Background(Colors.Grey.Lighten2).Padding(3).Text("F.O.B. POINT").Bold();

                table.Cell().BorderRight(1).Padding(3).Text(_encabezado.Orden_Compra_Cliente);
                table.Cell().BorderRight(1).Padding(3).Text(_encabezado.Condiciones_Pago);
                table.Cell().BorderRight(1).Padding(3).Text(_encabezado.Metodo_Envio);
                table.Cell().Padding(3).Text(_encabezado.Punto_Entrega);
            });
        }

        // MÉTODO PARA LA SEGUNDA TABLA (CON BORDE SUPERIOR PARA LA SEPARACIÓN)
        void ComposeInfoBar2(IContainer container)
        {
           
            // Se añade un BorderTop(1) para crear la línea divisoria horizontal
            container.BorderTop(1).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2.5f); columns.RelativeColumn(2.5f);
                    columns.RelativeColumn(1.5f); columns.RelativeColumn(1.5f);
                });

                table.Cell().BorderBottom(1).BorderRight(1).Background(Colors.Grey.Lighten2).Padding(3).Text("ORDERED BY").Bold();
                table.Cell().BorderBottom(1).BorderRight(1).Background(Colors.Grey.Lighten2).Padding(3).Text("SALES REPRESENTATIVE").Bold();
                table.Cell().BorderBottom(1).BorderRight(1).Background(Colors.Grey.Lighten2).Padding(3).Text("ORDER DATE").Bold();
                table.Cell().BorderBottom(1).Background(Colors.Grey.Lighten2).Padding(3).Text("CUSTOMER ID").Bold();

                table.Cell().BorderRight(1).Padding(3).Text(_encabezado.Solicitado_Por);
                table.Cell().BorderRight(1).Padding(3).Text("");
                table.Cell().BorderRight(1).Padding(3).Text(_encabezado.Orden_Fecha.ToString("dd/MM/yyyy"));
                table.Cell().Padding(3).Text(_encabezado.Cliente_Codigo);
            });
        }

        void ComposeInfoBlock(IContainer container)
        {
            // Dibuja un borde exterior único.
            container.Border(1).Column(col =>
            {
                // Asegura que no haya espacio vertical entre las tablas.
                col.Spacing(0);

                // Coloca la primera tabla (sin su propio borde).
                col.Item().Element(ComposeInfoBar1);

                // Coloca la segunda tabla (sin su propio borde).
                col.Item().Element(ComposeInfoBar2);
            });
        }

        void ComposeDetailsTable(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(45); // ORDER
                    columns.ConstantColumn(20); // LN
                    columns.ConstantColumn(20); // DL
                    columns.RelativeColumn(0.8f); // ORDERED
                    columns.RelativeColumn(0.8f); // SHIPPED
                    columns.RelativeColumn(1.2f); // PART/LOT
                    columns.RelativeColumn(1.8f); // DESC/COMMENTS
                    columns.ConstantColumn(30);  // UNIT
                    columns.RelativeColumn(0.8f); // LOT QTY
                    columns.RelativeColumn(1f);   // EXT PRICE
                });

                table.Header(header =>
                {
                    static IContainer HeaderCellStyle(IContainer c) => c.Border(1).Background(Colors.Grey.Lighten2).Padding(3).AlignCenter();

                    // Primera Fila del Header
                    header.Cell().RowSpan(2).Element(HeaderCellStyle).Text("ORDER").Bold();
                    header.Cell().RowSpan(2).Element(HeaderCellStyle).Text("LN").Bold();
                    header.Cell().RowSpan(2).Element(HeaderCellStyle).Text("DL").Bold();
                    header.Cell().ColumnSpan(2).Element(HeaderCellStyle).Text("QUANTITY").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("PART IDENTIFIER").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("DESCRIPTION").Bold();
                    header.Cell().RowSpan(2).Element(HeaderCellStyle).Text("UNIT").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("UNIT PRICE").Bold();

                    header.Cell().RowSpan(2).Element(HeaderCellStyle).Text("EXTENDED PRICE").Bold();


                    // Segunda Fila del Header
                    header.Cell().Element(HeaderCellStyle).Text("ORDERED").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("SHIPPED").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("LOT / SERIAL NUMBER").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("COMMENTS").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("LOT QUANTITY").Bold();
                });

                // Estilo para celdas de datos: Borde superior y bordes laterales
                static IContainer DataCellStyle(IContainer c, bool last = false)
                {
                    var container = c.BorderTop(1).BorderLeft(1).PaddingHorizontal(3).PaddingVertical(2);
                    if (last) // La última celda de la fila también necesita borde derecho
                        container = container.BorderRight(1);
                    return container;
                }
                var culture = new CultureInfo("en-US");

                foreach (var item in _items)
                {
                    table.Cell().Element(c => DataCellStyle(c)).Text(_encabezado.Orden_Numero);
                    table.Cell().Element(c => DataCellStyle(c)).Text(item.Linea_Numero);
                    table.Cell().Element(c => DataCellStyle(c)).Text(item.entrega_numero);
                    table.Cell().Element(c => DataCellStyle(c)).AlignRight().Text(item.Cantidad_ordenada.ToString("N4", culture));
                    table.Cell().Element(c => DataCellStyle(c)).AlignRight().Text(item.Cantidad_Enviada.ToString("N4", culture));
                    table.Cell().Element(c => DataCellStyle(c)).Column(column =>
                    {
                        column.Item().Text(item.Producto_Codigo);
                        column.Item().Text($"Lot: {item.Lote_Numero}");
                    });
                    table.Cell().Element(c => DataCellStyle(c)).Column(column =>
                    {
                        column.Item().Text(item.Producto_Descripcion);
                        column.Item().Text("");
                    });
                    table.Cell().Element(c => DataCellStyle(c)).AlignCenter().Text(item.Unidad_Medida);
                    table.Cell().Element(c => DataCellStyle(c)).AlignRight().Text(item.Precio_Unitario.ToString("N4", culture));
                    table.Cell().Element(c => DataCellStyle(c, true)).AlignRight().Text($"$ {item.Subtotal_Linea.ToString("N4", culture)}"); // Celda final
                }

                for (int j = 0; j < 10; j++)
                {
                    var cell = table.Cell().ExtendVertical().BorderLeft(1);

                    if (j == 9)
                    {
                        cell.BorderRight(1);
                    }
                }
            });
        }

        void ComposeFooter(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Element(ComposeTotals);

                col.Item().PaddingTop(10).AlignLeft().Column(qrCol =>
                {
                    qrCol.Item()
                        .Width(120).Height(120) 
                        .AlignCenter().AlignMiddle()
                        .Image(Convert.FromBase64String(_qrB64));

                    qrCol.Item().AlignLeft().Text($"Security Code: {_signatureFirst6}").FontSize(9);

                    qrCol.Item().AlignLeft().Text($"Digital Signature Date: {_dateSentToDgi}").FontSize(9);
                });

                // Paginación
                col.Item().PaddingTop(5).AlignRight().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        }



        void ComposeTotals(IContainer container)
        {
            // El borde superior cierra la tabla de detalles
            container.BorderTop(1).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(); columns.RelativeColumn(); columns.RelativeColumn();
                    columns.RelativeColumn(); columns.RelativeColumn(); columns.RelativeColumn();
                    columns.RelativeColumn(); columns.RelativeColumn();
                });

                static IContainer HeaderCellStyle(IContainer c) => c.Background(Colors.Grey.Lighten2).Padding(3);

                table.Cell().Element(HeaderCellStyle).Text("LINE ITEM TOTALS").Bold();
                table.Cell().Element(HeaderCellStyle).Text("DISCOUNT").Bold();
                table.Cell().Element(HeaderCellStyle).Text("SUB TOTAL").Bold();
                table.Cell().Element(HeaderCellStyle).Text("FREIGHT").Bold();
                table.Cell().Element(HeaderCellStyle).Text("TAXABLE AMOUNT").Bold();
                table.Cell().Element(HeaderCellStyle).Text("TAX").Bold();
                table.Cell().Element(HeaderCellStyle).Text("MISC").Bold();
                table.Cell().Element(HeaderCellStyle).Text("INVOICE TOTAL").Bold();

                var culture = new CultureInfo("en-US");
                var lineTotal = _items.Sum(x => x.Subtotal_Linea);
                var discount = 0.00m;
                var subTotal = lineTotal - discount;
                var tax = 0.00m;
                var invoiceTotal = subTotal + tax;

                table.Cell().Padding(3).Text(lineTotal.ToString("N4", culture));
                table.Cell().Padding(3).Text(discount.ToString("N4", culture));
                table.Cell().Padding(3).Text(subTotal.ToString("N4", culture));
                table.Cell().Padding(3).Text("0.0000");
                table.Cell().Padding(3).Text(tax.ToString("N4", culture));
                table.Cell().Padding(3).Text(tax.ToString("N4", culture));
                table.Cell().Padding(3).Text("0.0000");
                table.Cell().Padding(3).Text($"$ {invoiceTotal.ToString("N4", culture)}");
            });
        }
    }

}
