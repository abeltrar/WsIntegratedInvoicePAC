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

        // Border constants
        private const float OUTER = 1f;
        private const string OUTER_COLOR = "#000000";
        private const float INNER = 0.5f;
        private const string INNER_COLOR = "#A0A0A0";

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
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8).FontColor(Color.FromHex("#000000")));

                page.Header().Column(headerCol =>
                {
                    headerCol.Item().Element(ComposeHeader);
                    headerCol.Item().PaddingTop(6).Element(ComposeBillToShipTo);
                    headerCol.Item().PaddingTop(4).Element(ComposeInfoBlock);
                });

                page.Content().PaddingTop(4).Element(ComposeDetailsTable);

                page.Footer().Element(ComposeFooter);
            });
        }

        // ══════════════════════════════════════════════
        //  SECTION 1 — Company header + Invoice box
        // ══════════════════════════════════════════════
        void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeColumn(1.6f).Row(lr =>
                {
                    if (_logo != null && _logo.Length > 0)
                        lr.ConstantColumn(75).Image(_logo).FitArea();
                    else
                        lr.ConstantColumn(75).Height(55).AlignMiddle().Text("B' LEATHER").Bold().FontSize(14);

                    lr.ConstantColumn(8);

                    lr.RelativeColumn().Column(tc =>
                    {
                        tc.Item().Height(4);
                        tc.Item().Text("B' LEATHER MANUFACTURING, INC.").Bold().FontSize(11);
                        tc.Item().Text("Parque Industrial y de Servicios Yaque, S.A.").FontSize(8);
                        tc.Item().Text("Ave. 27 de Febrero esquina Calle 2, Ensanche Bermudez").FontSize(8);
                        tc.Item().Text("Santiago de los Caballeros, Dominican Republic 51091").FontSize(8);
                        tc.Item().Text("Phone +1 (809) 575-7000").FontSize(8);
                        tc.Item().Text("RNC 1-30-72334-6").FontSize(8);
                    });
                });

                row.RelativeColumn(1f).Column(rc =>
                {
                    string titulo = (_encabezado.Tipo_Factura == "CU" || _encabezado.Tipo_Factura == "FR")
                        ? "INVOICE" : "ELECTRONIC CREDIT NOTE";

                    rc.Item().AlignCenter().Text(titulo).Bold().FontSize(16);
                    rc.Item().PaddingTop(3);

                    rc.Item()
                        .BorderColor(Color.FromHex(OUTER_COLOR))
                        .BorderTop(OUTER).BorderBottom(OUTER).BorderLeft(OUTER).BorderRight(OUTER)
                        .Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });

                            t.Cell().Background(Color.FromHex("#D9D9D9")).BorderColor(Color.FromHex(INNER_COLOR)).BorderBottom(INNER).BorderRight(INNER).Padding(2).AlignCenter().Text("INVOICE #").Bold().FontSize(8);
                            t.Cell().Background(Color.FromHex("#D9D9D9")).BorderColor(Color.FromHex(INNER_COLOR)).BorderBottom(INNER).BorderRight(INNER).Padding(2).AlignCenter().Text("INVOICE DATE").Bold().FontSize(8);
                            t.Cell().Background(Color.FromHex("#D9D9D9")).BorderColor(Color.FromHex(INNER_COLOR)).BorderBottom(INNER).Padding(2).AlignCenter().Text("PAGE").Bold().FontSize(8);

                            t.Cell().BorderColor(Color.FromHex(INNER_COLOR)).BorderBottom(INNER).BorderRight(INNER).Padding(2).AlignCenter().Text(_encabezado.Factura_Numero).FontSize(8);
                            t.Cell().BorderColor(Color.FromHex(INNER_COLOR)).BorderBottom(INNER).BorderRight(INNER).Padding(2).AlignCenter().Text(_encabezado.Factura_Fecha_Emision.ToString("dd/MM/yyyy")).FontSize(8);
                            t.Cell().BorderColor(Color.FromHex(INNER_COLOR)).BorderBottom(INNER).Padding(2).AlignCenter().Text(x =>
                            {
                                x.CurrentPageNumber().Style(TextStyle.Default.FontSize(8));
                                x.Span(" of ").Style(TextStyle.Default.FontSize(8));
                                x.TotalPages().Style(TextStyle.Default.FontSize(8));
                            });

                            if (_encabezado.Tipo_Factura == "CU" || _encabezado.Tipo_Factura == "FR")
                            {
                                t.Cell().Background(Color.FromHex("#D9D9D9")).BorderColor(Color.FromHex(INNER_COLOR)).BorderBottom(INNER).BorderRight(INNER).Padding(2).AlignCenter().Text("NCF #").Bold().FontSize(8);
                                t.Cell().ColumnSpan(2).Background(Color.FromHex("#D9D9D9")).BorderColor(Color.FromHex(INNER_COLOR)).BorderBottom(INNER).Padding(2).AlignCenter().Text("VALID UNTIL").Bold().FontSize(8);
                                t.Cell().BorderColor(Color.FromHex(INNER_COLOR)).BorderRight(INNER).Padding(2).AlignCenter().Text(_encabezado.NCF).FontSize(8);
                                t.Cell().ColumnSpan(2).Padding(2).AlignCenter().Text(_fechaVencimiento).FontSize(8);
                            }
                            else if (_encabezado.Tipo_Factura == "CR")
                            {
                                t.Cell().Background(Color.FromHex("#D9D9D9")).BorderColor(Color.FromHex(INNER_COLOR)).BorderBottom(INNER).BorderRight(INNER).Padding(2).AlignCenter().Text("NCF #").Bold().FontSize(8);
                                t.Cell().ColumnSpan(2).Background(Color.FromHex("#D9D9D9")).BorderColor(Color.FromHex(INNER_COLOR)).BorderBottom(INNER).Padding(2).AlignCenter().Text("NCF MODIFIED #").Bold().FontSize(8);
                                t.Cell().BorderColor(Color.FromHex(INNER_COLOR)).BorderRight(INNER).Padding(2).AlignCenter().Text(_encabezado.NCF).FontSize(8);
                                t.Cell().ColumnSpan(2).Padding(2).AlignCenter().Column(c =>
                                {
                                    c.Item().Text(_encabezado.Factura_Afectada_NC).FontSize(8);
                                    c.Item().Text("Cancels the modified NCF").Bold().FontSize(7);
                                });
                            }
                        });
                });
            });
        }

        // ══════════════════════════════════════════════
        //  SECTION 2 — Ship To / Bill To
        // ══════════════════════════════════════════════
        void ComposeBillToShipTo(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeColumn().Padding(4).Column(c =>
                {
                    c.Item().Text("SHIP TO:").Bold().FontSize(8);
                    c.Item().Text(_encabezado.nombre_envio).Bold().FontSize(8);
                    c.Item().Text(_encabezado.Direccion_Envio1).FontSize(8);
                    c.Item().Text(_encabezado.Direccion_Envio2).FontSize(8);
                    c.Item().Text(_encabezado.direccion_envio3).FontSize(8);
                    c.Item().Text(_encabezado.direccion_envio4).FontSize(8);
                    c.Item().Text(_encabezado.Cliente_Documento_Identidad).FontSize(8);
                });
                row.ConstantColumn(10);
                row.RelativeColumn().Padding(4).Column(c =>
                {
                    c.Item().Text("BILL TO:").Bold().FontSize(8);
                    c.Item().Text(_encabezado.Cliente_Nombre).Bold().FontSize(8);
                    c.Item().Text(_encabezado.Cliente_Direccion1).FontSize(8);
                    c.Item().Text(_encabezado.Cliente_Direccion2).FontSize(8);
                    c.Item().Text("").FontSize(8);
                    c.Item().Text("").FontSize(8);
                    c.Item().Text(_encabezado.Cliente_Documento_Identidad).FontSize(8);

                });
            });
        }

        // ══════════════════════════════════════════════
        //  SECTION 3 — Info bars
        // ══════════════════════════════════════════════
        void ComposeInfoBlock(IContainer container)
        {
            container
                .BorderColor(Color.FromHex(OUTER_COLOR))
                .BorderTop(OUTER).BorderBottom(OUTER).BorderLeft(OUTER).BorderRight(OUTER)
                .Column(col =>
                {
                    col.Spacing(0);

                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        t.Cell().Background(Color.FromHex("#D9D9D9")).BorderColor(Color.FromHex(INNER_COLOR)).BorderBottom(INNER).BorderRight(INNER).Padding(3).AlignCenter().Text("CUSTOMER PO NUMBER").Bold().FontSize(8);
                        t.Cell().Background(Color.FromHex("#D9D9D9")).BorderColor(Color.FromHex(INNER_COLOR)).BorderBottom(INNER).BorderRight(INNER).Padding(3).AlignCenter().Text("PAYMENT TERMS").Bold().FontSize(8);
                        t.Cell().Background(Color.FromHex("#D9D9D9")).BorderColor(Color.FromHex(INNER_COLOR)).BorderBottom(INNER).BorderRight(INNER).Padding(3).AlignCenter().Text("SHIP VIA").Bold().FontSize(8);
                        t.Cell().Background(Color.FromHex("#D9D9D9")).BorderColor(Color.FromHex(INNER_COLOR)).BorderBottom(INNER).Padding(3).AlignCenter().Text("FOB POINT").Bold().FontSize(8);

                        t.Cell().BorderColor(Color.FromHex(INNER_COLOR)).BorderRight(INNER).Padding(3).AlignCenter().Text(_encabezado.Orden_Compra_Cliente).FontSize(8);
                        t.Cell().BorderColor(Color.FromHex(INNER_COLOR)).BorderRight(INNER).Padding(3).AlignCenter().Text(_encabezado.Condiciones_Pago).FontSize(8);
                        t.Cell().BorderColor(Color.FromHex(INNER_COLOR)).BorderRight(INNER).Padding(3).AlignCenter().Text(_encabezado.Metodo_Envio).FontSize(8);
                        t.Cell().Padding(3).AlignCenter().Text(_encabezado.Punto_Entrega).FontSize(8);
                    });

                    col.Item().BorderColor(Color.FromHex(INNER_COLOR)).BorderTop(INNER).Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2f);
                            c.RelativeColumn(2f);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(1f);
                        });

                        t.Cell().Background(Color.FromHex("#D9D9D9")).BorderColor(Color.FromHex(INNER_COLOR)).BorderBottom(INNER).BorderRight(INNER).Padding(3).AlignCenter().Text("ORDERED BY").Bold().FontSize(8);
                        t.Cell().Background(Color.FromHex("#D9D9D9")).BorderColor(Color.FromHex(INNER_COLOR)).BorderBottom(INNER).BorderRight(INNER).Padding(3).AlignCenter().Text("SALES REPRESENTATIVE").Bold().FontSize(8);
                        t.Cell().Background(Color.FromHex("#D9D9D9")).BorderColor(Color.FromHex(INNER_COLOR)).BorderBottom(INNER).BorderRight(INNER).Padding(3).AlignCenter().Text("OUR ORDER NUMBER").Bold().FontSize(8);
                        t.Cell().Background(Color.FromHex("#D9D9D9")).BorderColor(Color.FromHex(INNER_COLOR)).BorderBottom(INNER).BorderRight(INNER).Padding(3).AlignCenter().Text("ORDER DATE").Bold().FontSize(8);
                        t.Cell().Background(Color.FromHex("#D9D9D9")).BorderColor(Color.FromHex(INNER_COLOR)).BorderBottom(INNER).Padding(3).AlignCenter().Text("CUSTOMER ID").Bold().FontSize(8);

                        t.Cell().BorderColor(Color.FromHex(INNER_COLOR)).BorderRight(INNER).Padding(3).AlignCenter().Text(_encabezado.Solicitado_Por).FontSize(8);
                        t.Cell().BorderColor(Color.FromHex(INNER_COLOR)).BorderRight(INNER).Padding(3).AlignCenter().Text("").FontSize(8);
                        t.Cell().BorderColor(Color.FromHex(INNER_COLOR)).BorderRight(INNER).Padding(3).AlignCenter().Text(_encabezado.Orden_Numero).FontSize(8);
                        t.Cell().BorderColor(Color.FromHex(INNER_COLOR)).BorderRight(INNER).Padding(3).AlignCenter().Text(_encabezado.Orden_Fecha.ToString("MM/dd/yyyy")).FontSize(8);
                        t.Cell().Padding(3).AlignCenter().Text(_encabezado.Cliente_Codigo).FontSize(8);
                    });
                });
        }

        // ══════════════════════════════════════════════
        //  SECTION 4+5 — ONE unified table: header rows + data rows
        // ══════════════════════════════════════════════
        void ComposeDetailsTable(IContainer container)
        {
            container
                .BorderColor(Color.FromHex(OUTER_COLOR))
                .BorderTop(OUTER).BorderLeft(OUTER).BorderRight(OUTER)
                .Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(18);    // LN
                        c.ConstantColumn(18);    // DL
                        c.RelativeColumn(1.2f);  // PART NUMBER / LOT NUMBER
                        c.RelativeColumn(2.2f);  // DESCRIPTION / NOTES
                        c.RelativeColumn(0.9f);  // ORDERED
                        c.RelativeColumn(0.9f);  // SHIPPED
                        c.ConstantColumn(40);    // UOM
                        c.RelativeColumn(1f);    // UNIT PRICE
                        c.RelativeColumn(1f);    // EXTENDED PRICE
                    });

                    // ── Header — 2 rows, repeats on every page ──────────────────
                    table.Header(header =>
                    {
                        IContainer H(IContainer c) =>
                            c.Background(Color.FromHex("#D9D9D9"))
                             .BorderColor(Color.FromHex(INNER_COLOR))
                             .BorderTop(INNER).BorderBottom(INNER).BorderLeft(INNER).BorderRight(INNER)
                             .Padding(3).AlignCenter().AlignMiddle();

                        header.Cell().RowSpan(2).Element(H).Text("LN").Bold().FontSize(8);
                        header.Cell().RowSpan(2).Element(H).Text("DL").Bold().FontSize(8);
                        header.Cell().Element(H).Text("PART NUMBER").Bold().FontSize(8);
                        header.Cell().Element(H).Text("DESCRIPTION").Bold().FontSize(8);
                        header.Cell().ColumnSpan(2).Element(H).Text("QUANTITY").Bold().FontSize(8);
                        header.Cell().RowSpan(2).Element(H).Text("UOM").Bold().FontSize(8);
                        header.Cell().RowSpan(2).Element(H).Text("UNIT PRICE").Bold().FontSize(8);
                        header.Cell().RowSpan(2).Element(H).Text("EXTENDED PRICE").Bold().FontSize(8);

                        header.Cell().Element(H).Text("LOT NUMBER").Bold().FontSize(8);
                        header.Cell().Element(H).Text("NOTES").Bold().FontSize(8);
                        header.Cell().Element(H).Text("ORDERED").Bold().FontSize(8);
                        header.Cell().Element(H).Text("SHIPPED").Bold().FontSize(8);
                    });

                    // ── Data rows ───────────────────────────────────────────────
                    var culture = new CultureInfo("en-US");

                    IContainer D(IContainer c) =>
                        c.BorderColor(Color.FromHex(INNER_COLOR))
                         .BorderTop(INNER).BorderBottom(INNER).BorderLeft(INNER).BorderRight(INNER)
                         .Padding(3);

                    foreach (var item in _items.OrderBy(x => int.TryParse(x.Linea_Numero, out var n) ? n : 0))
                    {
                        table.Cell().Element(D).AlignCenter().AlignMiddle()
                            .Text(item.Linea_Numero).FontSize(8);

                        table.Cell().Element(D).AlignCenter().AlignMiddle()
                            .Text(item.entrega_numero).FontSize(8);

                        table.Cell().Element(D).Column(col =>
                        {
                            col.Item().Text(item.Producto_Codigo).FontSize(8);
                            var lotes = item.Lote_Numero?.Split(',', StringSplitOptions.RemoveEmptyEntries);
                            if (lotes != null)
                                foreach (var lote in lotes)
                                {
                                    var clean = lote.Replace("LOT", "").Replace("Lot", "")
                                                    .Trim().TrimStart(':').Trim();
                                    col.Item().PaddingLeft(6)
                                        .Text("LOT: " + clean)
                                        .FontSize(7).FontColor(Color.FromHex("#555555"));
                                }
                        });

                        table.Cell().Element(D).Column(col =>
                        {
                            col.Item().Text(item.Producto_Descripcion).FontSize(8);
                            if (!string.IsNullOrWhiteSpace(item.Descripcion2))
                                col.Item().Text(item.Descripcion2)
                                    .FontSize(7).FontColor(Color.FromHex("#555555"));
                        });

                        table.Cell().Element(D).AlignRight().AlignMiddle()
                            .Text(item.Cantidad_ordenada.ToString("N2", culture)).FontSize(8);

                        table.Cell().Element(D).AlignRight().AlignMiddle()
                            .Text(item.Cantidad_Enviada.ToString("N2", culture)).FontSize(8);

                        table.Cell().Element(D).AlignCenter().AlignMiddle()
                            .Text(item.Unidad_Medida).FontSize(8);

                        table.Cell().Element(D).AlignMiddle().AlignCenter().Text(t =>
                        {
                            t.Span("US$ ").FontSize(7).FontColor(Color.FromHex("#555555"));
                            t.Span(item.Precio_Unitario.ToString("N2", culture)).FontSize(8);
                        });

                        table.Cell().Element(D).AlignMiddle().AlignCenter().Text(t =>
                        {
                            t.Span("US$ ").FontSize(7).FontColor(Color.FromHex("#555555"));
                            t.Span(item.Subtotal_Linea.ToString("N2", culture)).FontSize(8);
                        });
                    }

                    // Filler cells — extiende líneas verticales al fondo (última página)
                    for (int j = 0; j < 9; j++)
                    {
                        var cell = table.Cell().ExtendVertical()
                                       .BorderColor(Color.FromHex(INNER_COLOR)).BorderLeft(INNER);
                        if (j == 8)
                            cell.BorderColor(Color.FromHex(INNER_COLOR)).BorderRight(INNER);
                    }

                    // Footer de tabla: línea de cierre en CADA página (1px negro = borde)
                    table.Footer(footer =>
                    {
                        for (int j = 0; j < 9; j++)
                            footer.Cell()
                                .Background(Color.FromHex(OUTER_COLOR))
                                .Height(OUTER);
                    });
                });
        }

        // ══════════════════════════════════════════════
        //  FOOTER — Notes + QR + Totales + Payment
        // ══════════════════════════════════════════════
        void ComposeFooter(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().PaddingTop(4).Element(ComposeNotes);

                col.Item().PaddingTop(5).Row(mainRow =>
                {
                    mainRow.ConstantColumn(110).Column(qrCol =>
                    {
                        qrCol.Item()
                            .Width(100)
                            .Image(Convert.FromBase64String(_qrB64)).FitWidth();

                        qrCol.Item().PaddingTop(4).Text($"Security Code: {_signatureFirst6}").FontSize(7);
                        qrCol.Item().Text("Digital Signature Date:").FontSize(7);
                        qrCol.Item().Text(_dateSentToDgi).FontSize(7);
                    });

                    mainRow.RelativeColumn();

                    mainRow.ConstantColumn(300).Column(rightCol =>
                    {
                        rightCol.Item().AlignRight().Width(200).Element(ComposeTotals);
                        rightCol.Item().PaddingTop(4).AlignRight().Width(300).Element(ComposePaymentInstructions);
                    });
                });

                col.Item().PaddingTop(5).AlignRight().Text(x =>
                {
                    x.Span("Page ").Style(TextStyle.Default.FontSize(8));
                    x.CurrentPageNumber().Style(TextStyle.Default.FontSize(8));
                    x.Span(" of ").Style(TextStyle.Default.FontSize(8));
                    x.TotalPages().Style(TextStyle.Default.FontSize(8));
                });
            });
        }

        void ComposeNotes(IContainer container)
        {
            container
                .BorderColor(Color.FromHex(OUTER_COLOR))
                .BorderTop(OUTER).BorderBottom(OUTER).BorderLeft(OUTER).BorderRight(OUTER)
                .Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(42);
                        c.RelativeColumn();
                    });

                    t.Cell().Background(Color.FromHex("#D9D9D9"))
                        .BorderColor(Color.FromHex(INNER_COLOR)).BorderRight(INNER)
                        .Padding(3).AlignMiddle()
                        .Text("NOTES:").Bold().FontSize(8);

                    t.Cell().Padding(3)
                        .Text(!string.IsNullOrWhiteSpace(_comentarios) ? _comentarios : " ").FontSize(8);
                });
        }

        void ComposeTotals(IContainer container)
        {
            var culture = new CultureInfo("en-US");
            var subTotal = _items.Sum(x => x.Subtotal_Linea);
            var tax = 0.00m;
            var freight = _encabezado.valor_flete ?? 0;
            var invoiceTotal = subTotal + tax + freight;

            container
                .BorderColor(Color.FromHex(OUTER_COLOR))
                .BorderTop(OUTER).BorderLeft(OUTER).BorderRight(OUTER)
                .Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn();   // label
                        c.ConstantColumn(32); // US$
                        c.ConstantColumn(60); // amount
                    });

                    void Row(string label, decimal amount)
                    {
                        t.Cell().Background(Color.FromHex("#D9D9D9"))
                            .BorderColor(Color.FromHex(INNER_COLOR)).BorderBottom(INNER)
                            .PaddingLeft(3).PaddingRight(3).PaddingTop(2).PaddingBottom(2)
                            .AlignLeft().Text(label).Bold().FontSize(8);
                        t.Cell()
                            .BorderColor(Color.FromHex(INNER_COLOR)).BorderBottom(INNER)
                            .PaddingLeft(3).PaddingRight(3).PaddingTop(2).PaddingBottom(2)
                            .AlignLeft().Text("US$").FontSize(8);
                        t.Cell()
                            .BorderColor(Color.FromHex(INNER_COLOR)).BorderBottom(INNER)
                            .PaddingLeft(3).PaddingRight(3).PaddingTop(2).PaddingBottom(2)
                            .AlignRight().Text(amount.ToString("N2", culture)).FontSize(8);
                    }

                    Row("SUB TOTAL", subTotal);
                    Row("FREIGHT", freight);
                    Row("TAXABLE AMOUNT", tax);
                    Row("TAX", tax);
                    Row("OTHER CHARGES", 0);

                    // INVOICE TOTAL
                    t.Cell().Background(Color.FromHex("#D9D9D9"))
                        .BorderColor(Color.FromHex(OUTER_COLOR)).BorderBottom(OUTER)
                        .PaddingLeft(3).PaddingRight(3).PaddingTop(2).PaddingBottom(2)
                        .AlignLeft().Text("INVOICE TOTAL").Bold().FontSize(8);
                    t.Cell().Background(Color.FromHex("#D9D9D9"))
                        .BorderColor(Color.FromHex(OUTER_COLOR)).BorderBottom(OUTER)
                        .PaddingLeft(3).PaddingRight(3).PaddingTop(2).PaddingBottom(2)
                        .AlignLeft().Text("US$").Bold().FontSize(8);
                    t.Cell().Background(Color.FromHex("#D9D9D9"))
                        .BorderColor(Color.FromHex(OUTER_COLOR)).BorderBottom(OUTER)
                        .PaddingLeft(3).PaddingRight(3).PaddingTop(2).PaddingBottom(2)
                        .AlignRight().Text(invoiceTotal.ToString("N2", culture)).Bold().FontSize(8);
                });
        }

        void ComposePaymentInstructions(IContainer container)
        {
            container
                .BorderColor(Color.FromHex(OUTER_COLOR))
                .BorderTop(OUTER).BorderBottom(OUTER).BorderLeft(OUTER).BorderRight(OUTER)
                .Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(90);  // label
                        c.RelativeColumn();    // value
                    });

                    t.Cell().ColumnSpan(2)
                        .BorderColor(Color.FromHex(INNER_COLOR)).BorderBottom(INNER)
                        .PaddingLeft(3).PaddingRight(3).PaddingTop(2).PaddingBottom(2)
                        .Text("PAYMENT INSTRUCTIONS:").Bold().FontSize(8);

                    void PayRow(string label, string value)
                    {
                        t.Cell().BorderColor(Color.FromHex(INNER_COLOR)).BorderRight(INNER)
                            .PaddingLeft(3).PaddingRight(3).PaddingTop(2).PaddingBottom(2)
                            .Text(label).Bold().FontSize(8);
                        t.Cell()
                            .PaddingLeft(3).PaddingRight(3).PaddingTop(2).PaddingBottom(2)
                            .Text(value).FontSize(8);
                    }

                    PayRow("ACCOUNT NAME:", "B' LEATHER MANUFACTURING, INC.");
                    PayRow("ACCOUNT NUMBER:", _encabezado.ACCT);
                    PayRow("BANK:", "CITIBANK, N.A.");
                    PayRow("BANK ADDRESS:", "111 WALL STREET. NEW YORK, NY USA 10043");
                    PayRow("ROUTING:", _encabezado.ABA.ToString().PadLeft(9, '0'));
                    PayRow("SWIFT:", _encabezado.SWIFT);
                });
        }
    }
}