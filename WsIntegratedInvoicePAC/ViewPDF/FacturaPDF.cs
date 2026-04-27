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
        private readonly string _comentarios;

        public FacturaPDF(FacturaData data)
        {
            _logo = data.Logo;
            _encabezado = data.Encabezado;
            _items = data.Items.ToList();
            _dateSentToDgi = data.DateSentToDgi;
            _signatureFirst6 = data.SignatureFirst6;
            _fechaVencimiento = data.FechaVencimiento;
            _qrB64 = data.QrB64;
            _comentarios = data.comentarios;

        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8).FontColor(Color.FromHex("#333333")));

                page.Header().Column(headerCol =>
                {
                    headerCol.Item().Element(ComposeHeader);
                    headerCol.Item().Element(ComposeBillToShipTo);
                    headerCol.Item().Element(ComposeInfoBlock);
                });

                page.Content()
                    .Column(col =>
                    {
                        col.Item().Element(ComposeContent);
                    });

                page.Footer()
                    .Element(ComposeFooter);
            });
        }

        void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeColumn(1.5f).Row(headerLeftRow =>
                {
                    if (_logo != null && _logo.Length > 0)
                        headerLeftRow.ConstantColumn(80).Image(_logo).FitArea();
                    else
                        headerLeftRow.ConstantColumn(80).Height(60).Text("B' LEATHER").Bold().FontSize(16);

                    headerLeftRow.ConstantColumn(10);

                    headerLeftRow.RelativeColumn().Column(textCol =>
                    {
                        textCol.Item().Height(5);
                        textCol.Item().Text("B' LEATHER MANUFACTURING, INC.").Bold().FontSize(10);
                        textCol.Item().Text("Parque Industrial y de Servicios Yaque, S.A.");
                        textCol.Item().Text("Ave. 27 de Febrero esquina Calle 2. Ensanche Bermudez.");
                        textCol.Item().Text("Santiago de los Caballeros, Dominican Republic 51091");
                        textCol.Item().Text("Phone +1 (809) 575-7000");
                        textCol.Item().Text("RNC 1-30-72334-6");
                    });
                });

                row.RelativeColumn(1).Column(col =>
                {
                    string nombre_factura = "";
                    if (_encabezado.Tipo_Factura == "CU" || _encabezado.Tipo_Factura == "FR")
                        nombre_factura = "INVOICE";
                    else
                        nombre_factura = "ELECTRONIC CREDIT NOTE";

                    col.Item().AlignCenter().Text(nombre_factura).Bold().FontSize(14);
                    col.Item().PaddingBottom(2);

                  
                    col.Item().BorderColor(Color.FromHex("#000000")).BorderBottom(0.5f).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderBottom(0.5f).BorderRight(0.5f).Background(Color.FromHex("#D9D9D9")).Padding(2).Text("INVOICE #").Bold();
                        table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderBottom(0.5f).BorderRight(0.5f).Background(Color.FromHex("#D9D9D9")).Padding(2).Text("INVOICE DATE").Bold();
                        table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderBottom(0.5f).Background(Color.FromHex("#D9D9D9")).Padding(2).Text("PAGE").Bold();

                        table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderBottom(0.5f).BorderRight(0.5f).Padding(2).Text(_encabezado.Factura_Numero);
                        table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderBottom(0.5f).BorderRight(0.5f).Padding(2).Text(_encabezado.Factura_Fecha_Emision.ToString("dd/MM/yyyy"));
                        table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderBottom(0.5f).Padding(2).Text(x =>
                        {
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });

                        if (_encabezado.Tipo_Factura == "CU" || _encabezado.Tipo_Factura == "FR")
                        {
                            table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderBottom(0.5f).BorderRight(0.5f).Background(Color.FromHex("#D9D9D9")).Padding(2).Text("NCF #").Bold();
                            table.Cell().ColumnSpan(2).BorderColor(Color.FromHex("#A0A0A0")).BorderBottom(0.5f).Background(Color.FromHex("#D9D9D9")).Padding(2).Text("VALID UNTIL").Bold();

                            // Fila 4: Valores NCF / Valid Until
                            table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderRight(0.5f).Padding(2).Text(_encabezado.NCF);
                            table.Cell().ColumnSpan(2).Padding(2).Text(_fechaVencimiento);
                        }
                        else if (_encabezado.Tipo_Factura == "CR")
                        {
                            table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderBottom(0.5f).BorderRight(0.5f).Background(Color.FromHex("#D9D9D9")).Padding(2).Text("NCF #").Bold();
                            table.Cell().ColumnSpan(2).BorderColor(Color.FromHex("#A0A0A0")).BorderBottom(0.5f).Background(Color.FromHex("#D9D9D9")).Padding(2).Text("NCF MODIFIED #").Bold();

                            table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderRight(0.5f).Padding(2).Text(_encabezado.NCF);
                            table.Cell().ColumnSpan(2).Padding(2).Column(c =>
                            {
                                c.Item().Text(_encabezado.Factura_Afectada_NC);
                                c.Item().Text("Cancels the modified NCF").Bold().FontSize(7);
                            });
                        }
                    });
                });
            });
        }

        void ComposeContent(IContainer container)
        {
            container
            .PaddingBottom(0) 
            .BorderColor(Color.FromHex("#A0A0A0"))
            .BorderBottom(0.5f) 
            .Element(ComposeDetailsTable);
        }

        void ComposeBillToShipTo(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeColumn().Padding(5).Column(c =>
                {
                    c.Item().Text("SHIP TO:").Bold();
                    c.Item().Text(_encabezado.Cliente_Nombre).Bold();
                    c.Item().Text(_encabezado.Direccion_Envio1);
                    c.Item().Text(_encabezado.Direccion_Envio2);
                    c.Item().Text("TAX ID");
                });
                row.ConstantColumn(10);
                row.RelativeColumn().Padding(5).Column(c =>
                {
                    c.Item().Text("BILL TO:").Bold();
                    c.Item().Text(_encabezado.Cliente_Nombre).Bold();
                    c.Item().Text(_encabezado.Cliente_Direccion1);
                    c.Item().Text(_encabezado.Cliente_Direccion2);
                    c.Item().Text("TAX ID");
                });
            });
        }

        void ComposeInfoBar1(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderBottom(0.5f).BorderRight(0.5f).Background(Color.FromHex("#D9D9D9")).Padding(3).AlignCenter().Text("CUSTOMER PO NUMBER").Bold();
                table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderBottom(0.5f).BorderRight(0.5f).Background(Color.FromHex("#D9D9D9")).Padding(3).AlignCenter().Text("PAYMENT TERMS").Bold();
                table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderBottom(0.5f).BorderRight(0.5f).Background(Color.FromHex("#D9D9D9")).Padding(3).AlignCenter().Text("SHIP VIA").Bold();
                table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderBottom(0.5f).Background(Color.FromHex("#D9D9D9")).Padding(3).AlignCenter().Text("FOB POINT").Bold();

                table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderRight(0.5f).Padding(3).AlignCenter().Text(_encabezado.Orden_Compra_Cliente);
                table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderRight(0.5f).Padding(3).AlignCenter().Text(_encabezado.Condiciones_Pago);
                table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderRight(0.5f).Padding(3).AlignCenter().Text(_encabezado.Metodo_Envio);
                table.Cell().Padding(3).AlignCenter().Text(_encabezado.Punto_Entrega);
            });
        }

        void ComposeInfoBar2(IContainer container)
        {
            container.BorderColor(Color.FromHex("#A0A0A0")).BorderTop(0.5f).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2f);
                    columns.RelativeColumn(2f);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1f);
                });

                table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderBottom(0.5f).BorderRight(0.5f).Background(Color.FromHex("#D9D9D9")).Padding(3).AlignCenter().Text("ORDERED BY").Bold();
                table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderBottom(0.5f).BorderRight(0.5f).Background(Color.FromHex("#D9D9D9")).Padding(3).AlignCenter().Text("SALES REPRESENTATIVE").Bold();
                table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderBottom(0.5f).BorderRight(0.5f).Background(Color.FromHex("#D9D9D9")).Padding(3).AlignCenter().Text("OUR ORDER NUMBER").Bold();
                table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderBottom(0.5f).BorderRight(0.5f).Background(Color.FromHex("#D9D9D9")).Padding(3).AlignCenter().Text("ORDER DATE").Bold();
                table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderBottom(0.5f).Background(Color.FromHex("#D9D9D9")).Padding(3).AlignCenter().Text("CUSTOMER ID").Bold();

                table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderRight(0.5f).Padding(3).AlignCenter().Text(_encabezado.Solicitado_Por);
                table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderRight(0.5f).Padding(3).AlignCenter().Text("");
                table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderRight(0.5f).Padding(3).AlignCenter().Text(_encabezado.Orden_Numero);
                table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderRight(0.5f).Padding(3).AlignCenter().Text(_encabezado.Orden_Fecha.ToString("MM/dd/yyyy"));
                table.Cell().Padding(3).AlignCenter().Text(_encabezado.Cliente_Codigo);
            });
        }

        void ComposeInfoBlock(IContainer container)
        {
            container.BorderColor(Color.FromHex("#A0A0A0"))
             .BorderTop(0.5f).BorderBottom(0.5f).BorderLeft(0.5f).BorderRight(0.5f)
             .Column(col =>
             {
                col.Spacing(0);
                col.Item().Element(ComposeInfoBar1);
                col.Item().Element(ComposeInfoBar2);
             });
        }

        void ComposeDetailsTable(IContainer container)
        {
            container.Table(table =>
            {
         
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(20);   
                    columns.ConstantColumn(20);   
                    columns.RelativeColumn(1.2f); 
                    columns.RelativeColumn(2f);   
                    columns.RelativeColumn(0.9f); 
                    columns.RelativeColumn(0.9f); 
                    columns.ConstantColumn(28);  
                    columns.RelativeColumn(0.9f); 
                    columns.RelativeColumn(1f);  
                });

                table.Header(header =>
                {
                    static IContainer HeaderCellStyle(IContainer c) =>
                        c.BorderColor(Color.FromHex("#A0A0A0"))
                         .BorderTop(0.5f).BorderBottom(0.5f).BorderLeft(0.5f).BorderRight(0.5f)
                         .Background(Color.FromHex("#D9D9D9")).Padding(3).AlignCenter();

                    header.Cell().RowSpan(2).Element(HeaderCellStyle).Text("LN").Bold();
                    header.Cell().RowSpan(2).Element(HeaderCellStyle).Text("DL").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("PART NUMBER").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("DESCRIPTION").Bold();
                    header.Cell().ColumnSpan(2).Element(HeaderCellStyle).Text("QUANTITY").Bold();
                    header.Cell().RowSpan(2).Element(HeaderCellStyle).Text("UOM").Bold();
                    header.Cell().RowSpan(2).Element(HeaderCellStyle).Text("UNIT PRICE").Bold();
                    header.Cell().RowSpan(2).Element(HeaderCellStyle).Text("EXTENDED PRICE").Bold();

                    header.Cell().Element(HeaderCellStyle).Text("LOT NUMBER").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("NOTES").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("ORDERED").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("SHIPPED").Bold();
                });

                var culture = new CultureInfo("en-US");

                foreach (var item in _items.OrderBy(x => int.TryParse(x.Linea_Numero, out var n) ? n : 0))

                {
                    table.Cell().BorderColor(Color.FromHex("#A0A0A0"))
                         .BorderTop(0.5f).BorderBottom(0.5f).BorderLeft(0.5f).BorderRight(0.5f)
                         .Padding(3).AlignCenter().AlignMiddle().Text(item.Linea_Numero);
                    table.Cell().BorderColor(Color.FromHex("#A0A0A0"))
                     .BorderTop(0.5f).BorderBottom(0.5f).BorderLeft(0.5f).BorderRight(0.5f)
                     .Padding(3).AlignCenter().AlignMiddle().Text(item.entrega_numero);

                    table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderTop(0.5f).BorderBottom(0.5f).BorderLeft(0.5f).BorderRight(0.5f).Padding(3).Padding(3).Column(column =>
                    {
                        column.Item().Text(item.Producto_Codigo);
                        var lotes = item.Lote_Numero?.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (lotes != null)
                        {
                            foreach (var lote in lotes)
                            {
                                column.Item().PaddingLeft(10).Text("LOT: " + lote.Replace("LOT", "").Replace("Lot", "").Trim().TrimStart(':').Trim());

                            }
                        }
                    });

                    table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderTop(0.5f).BorderBottom(0.5f).BorderLeft(0.5f).BorderRight(0.5f).Padding(3).Padding(3).Column(column =>
                    {

                        column.Item().Text(item.Producto_Descripcion);
                        if (!string.IsNullOrWhiteSpace(item.Descripcion2))
                            column.Item().Text(item.Descripcion2).FontSize(7).FontColor(Color.FromHex("#666666"));

                    });

                    table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderTop(0.5f).BorderBottom(0.5f).BorderLeft(0.5f).BorderRight(0.5f).Padding(3).Padding(3).AlignRight().AlignMiddle().Text(item.Cantidad_ordenada.ToString("N2", culture));
                    table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderTop(0.5f).BorderBottom(0.5f).BorderLeft(0.5f).BorderRight(0.5f).Padding(3).Padding(3).AlignRight().AlignMiddle().Text(item.Cantidad_Enviada.ToString("N2", culture));
                    table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderTop(0.5f).BorderBottom(0.5f).BorderLeft(0.5f).BorderRight(0.5f).Padding(3).Padding(3).AlignCenter().AlignMiddle().Text(item.Unidad_Medida);

                    table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderTop(0.5f).BorderBottom(0.5f).BorderLeft(0.5f).BorderRight(0.5f).Padding(3).Padding(3).AlignMiddle().Row(r =>
                    {
              
                        r.RelativeColumn().AlignCenter().Text(item.Precio_Unitario.ToString("N2", culture));
                    });

                    table.Cell().BorderColor(Color.FromHex("#A0A0A0")).BorderTop(0.5f).BorderBottom(0.5f).BorderLeft(0.5f).BorderRight(0.5f).Padding(3).Padding(3).AlignMiddle().Row(r =>
                    {
                       
                        r.RelativeColumn().AlignCenter().Text(item.Subtotal_Linea.ToString("N2", culture));
                    });
                }


                for (int j = 0; j < 9; j++)
                {
                    var cell = table.Cell().ExtendVertical().BorderColor(Color.FromHex("#A0A0A0")).BorderLeft(0.5f);
                    if (j == 8)
                        cell.BorderColor(Color.FromHex("#A0A0A0")).BorderRight(0.5f);
                }
            });
        }


        void ComposeNotes(IContainer container)
        {
            container
                .BorderColor(Color.FromHex("#A0A0A0"))
                .BorderTop(0.5f).BorderBottom(0.5f)
                .BorderLeft(0.5f).BorderRight(0.5f)
                .Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(45);  
                        columns.RelativeColumn();    
                    });

                    table.Cell()
                        .Background(Color.FromHex("#D9D9D9"))
                        .BorderColor(Color.FromHex("#A0A0A0")).BorderRight(0.5f)
                        .Padding(3)
                        .Text("NOTES:").Bold();

                    table.Cell()
                        .Padding(3)
                        .Text(!string.IsNullOrWhiteSpace(_comentarios) ? _comentarios : " ");
                });
        }

        void ComposeFooter(IContainer container)
        {
            container.Column(col =>
            {

                col.Item().PaddingTop(5).Element(ComposeNotes);

                col.Item().PaddingTop(5).Row(mainRow =>
                {
                    mainRow.ConstantColumn(120).Column(qrCol =>
                    {
                        qrCol.Item()
                            .BorderColor(Color.FromHex("#A0A0A0"))
                            .BorderTop(0.5f).BorderBottom(0.5f)
                            .BorderLeft(0.5f).BorderRight(0.5f)
                            .Padding(2)                    // padding mínimo, solo 3
                            .Image(Convert.FromBase64String(_qrB64))
                            .FitArea();                    // ocupa todo el espacio disponible del contenedor

                        qrCol.Item().PaddingTop(4).AlignLeft()
                            .Text($"Security Code: {_signatureFirst6}").FontSize(8);
                        qrCol.Item().AlignLeft()
                            .Text("Digital Signature Date:").FontSize(8);
                        qrCol.Item().AlignLeft()
                            .Text(_dateSentToDgi).FontSize(8);
                    });

                    // Columna derecha: Totales arriba + Payment Instructions abajo
                    mainRow.RelativeColumn().Column(rightCol =>
                    {
                        // Totales
                        rightCol.Item().AlignRight().Element(ComposeTotals);

                        // Payment Instructions
                        rightCol.Item().PaddingTop(5).AlignRight()
                            .BorderColor(Color.FromHex("#A0A0A0"))
                            .BorderTop(0.5f).BorderBottom(0.5f)
                            .BorderLeft(0.5f).BorderRight(0.5f)
                            .Table(payTable =>
                            {
                                payTable.ColumnsDefinition(cols =>
                                {
                                    cols.ConstantColumn(105);
                                    cols.ConstantColumn(195);
                                });

                                static IContainer PayHeaderStyle(IContainer c) =>
                                    c.BorderColor(Color.FromHex("#A0A0A0")).BorderBottom(0.5f).Padding(3);
                                static IContainer PayLabelStyle(IContainer c) =>
                                    c.BorderColor(Color.FromHex("#A0A0A0")).BorderRight(0.5f).Padding(3);
                                static IContainer PayValueStyle(IContainer c) =>
                                    c.Padding(3);

                                payTable.Cell().ColumnSpan(2).Element(PayHeaderStyle)
                                    .Text("PAYMENT INSTRUCTIONS:").Bold().FontSize(8);

                                payTable.Cell().Element(PayLabelStyle).Text("ACCOUNT NAME:").Bold().FontSize(8);
                                payTable.Cell().Element(PayValueStyle).Text("B' LEATHER MANUFACTURING, INC.").FontSize(8);

                                payTable.Cell().Element(PayLabelStyle).Text("ACCOUNT NUMBER:").Bold().FontSize(8);
                                payTable.Cell().Element(PayValueStyle).Text(_encabezado.ACCT).FontSize(8);

                                payTable.Cell().Element(PayLabelStyle).Text("BANK:").Bold().FontSize(8);
                                payTable.Cell().Element(PayValueStyle).Text("CITIBANK, N.A.").FontSize(8);

                                payTable.Cell().Element(PayLabelStyle).Text("BANK ADDRESS:").Bold().FontSize(8);
                                payTable.Cell().Element(PayValueStyle)
                                    .Text("111 WALL STREET. NEW YORK, NY USA 10043").FontSize(8);

                                payTable.Cell().Element(PayLabelStyle).Text("ROUTING:").Bold().FontSize(8);
                                payTable.Cell().Element(PayValueStyle)
                                    .Text(_encabezado.ABA.ToString().PadLeft(9, '0')).FontSize(8);

                                payTable.Cell().Element(PayLabelStyle).Text("SWIFT:").Bold().FontSize(8);
                                payTable.Cell().Element(PayValueStyle).Text(_encabezado.SWIFT).FontSize(8);
                            });
                    });
                });

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
            var culture = new CultureInfo("en-US");
            var lineTotal = _items.Sum(x => x.Subtotal_Linea);
            var subTotal = lineTotal;
            var tax = 0.00m;
            var freight = _encabezado.valor_flete ?? 0;
            var invoiceTotal = subTotal + tax + freight;

            // Una sola definición de borde uniforme para toda la tabla
            container
                .BorderColor(Color.FromHex("#A0A0A0"))
                .BorderTop(0.5f).BorderBottom(0.5f)
                .BorderLeft(0.5f).BorderRight(0.5f)
                .Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(130);
                        columns.ConstantColumn(40);
                        columns.ConstantColumn(70);
                    });

                    // Separador interno entre celdas: solo BorderBottom en cada fila excepto la última
                    static IContainer LabelStyle(IContainer c) =>
                        c.BorderColor(Color.FromHex("#A0A0A0")).BorderBottom(0.5f)
                         .Background(Color.FromHex("#D9D9D9")).Padding(3).AlignLeft();
                    static IContainer CurrencyStyle(IContainer c) =>
                        c.BorderColor(Color.FromHex("#A0A0A0")).BorderBottom(0.5f)
                         .Padding(3).AlignLeft();
                    static IContainer AmountStyle(IContainer c) =>
                        c.BorderColor(Color.FromHex("#A0A0A0")).BorderBottom(0.5f)
                         .Padding(3).AlignRight();

                    // Última fila sin BorderBottom (el borde exterior del contenedor ya lo cierra)
                    static IContainer LabelLastStyle(IContainer c) =>
                        c.Background(Color.FromHex("#D9D9D9")).Padding(3).AlignLeft();
                    static IContainer CurrencyLastStyle(IContainer c) =>
                        c.Background(Color.FromHex("#D9D9D9")).Padding(3).AlignLeft();
                    static IContainer AmountLastStyle(IContainer c) =>
                        c.Background(Color.FromHex("#D9D9D9")).Padding(3).AlignRight();

                    table.Cell().Element(LabelStyle).Text("SUB TOTAL").Bold();
                    table.Cell().Element(CurrencyStyle).Text("US$");
                    table.Cell().Element(AmountStyle).Text(subTotal.ToString("N2", culture));

                    table.Cell().Element(LabelStyle).Text("FREIGHT").Bold();
                    table.Cell().Element(CurrencyStyle).Text("US$");
                    table.Cell().Element(AmountStyle).Text(freight.ToString("N2", culture));

                    table.Cell().Element(LabelStyle).Text("TAXABLE AMOUNT").Bold();
                    table.Cell().Element(CurrencyStyle).Text("US$");
                    table.Cell().Element(AmountStyle).Text(tax.ToString("N2", culture));

                    table.Cell().Element(LabelStyle).Text("TAX").Bold();
                    table.Cell().Element(CurrencyStyle).Text("US$");
                    table.Cell().Element(AmountStyle).Text(tax.ToString("N2", culture));

                    table.Cell().Element(LabelStyle).Text("OTHER CHARGES").Bold();
                    table.Cell().Element(CurrencyStyle).Text("US$");
                    table.Cell().Element(AmountStyle).Text("0.00");

                    // INVOICE TOTAL — última fila, sin BorderBottom interior
                    table.Cell().Element(LabelLastStyle).Text("INVOICE TOTAL").Bold();
                    table.Cell().Element(CurrencyLastStyle).Text("US$").Bold();
                    table.Cell().Element(AmountLastStyle).Text(invoiceTotal.ToString("N2", culture)).Bold();
                });
        }

    }

}