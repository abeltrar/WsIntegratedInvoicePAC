using System.Globalization;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using WsIntegratedInvoicePAC.Models;

namespace WsIntegratedInvoicePAC.ViewPDF
{
    public static class FacturaPDFStamper
    {
        private const float PAGE_MARGIN = 20f;
        private const float TOTALS_WIDTH = 200f;
        private const float PAYMENT_WIDTH = 300f;
        private const float GAP = 4f;
        // Payment: 1 header row + 6 data rows, avg ~14.5pt each (8pt font + 4pt padding + leading)
        private const float PAYMENT_SECTION_HEIGHT = 102f;

        private static readonly Color GrayBg = new DeviceRgb(0xD9, 0xD9, 0xD9);
        private static readonly Border OuterBorder = new SolidBorder(ColorConstants.BLACK, 1f);
        private static readonly Border InnerBorder = new SolidBorder(new DeviceRgb(160, 160, 160), 0.5f);

        /// <summary>
        /// Abre el PDF generado por QuestPDF y estampa los totales + instrucciones de pago
        /// en posición absoluta en el fondo-derecho de la última página.
        /// </summary>
        public static byte[] AgregarTotales(
            byte[] pdfBytes,
            FE_Invoice_Transaccion encabezado,
            IEnumerable<ItemFactura> items)
        {
            var culture = new CultureInfo("en-US");
            var subTotal = items.Sum(x => x.Subtotal_Linea);
            var freight = encabezado.valor_flete ?? 0m;
            const decimal tax = 0m;
            var invoiceTotal = subTotal + tax + freight;

            using var input = new MemoryStream(pdfBytes);
            using var output = new MemoryStream();

            var pdfDoc = new PdfDocument(new PdfReader(input), new PdfWriter(output));
            var document = new Document(pdfDoc);
            document.SetMargins(0f, 0f, 0f, 0f);

            int lastPageNum = pdfDoc.GetNumberOfPages();
            float pageWidth = pdfDoc.GetPage(lastPageNum).GetPageSize().GetWidth();

            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            // Posiciones desde el fondo de la página (iText7: y=0 en el borde inferior)
            float paymentBottom = PAGE_MARGIN;
            float totalsBottom = paymentBottom + PAYMENT_SECTION_HEIGHT + GAP;
            float paymentLeft = pageWidth - PAGE_MARGIN - PAYMENT_WIDTH;
            float totalsLeft = pageWidth - PAGE_MARGIN - TOTALS_WIDTH;

            var totalsTable = BuildTotalsTable(font, fontBold, culture, subTotal, freight, tax, invoiceTotal);
            totalsTable.SetFixedPosition(lastPageNum, totalsLeft, totalsBottom, TOTALS_WIDTH);
            document.Add(totalsTable);

            var paymentTable = BuildPaymentTable(font, fontBold, encabezado);
            paymentTable.SetFixedPosition(lastPageNum, paymentLeft, paymentBottom, PAYMENT_WIDTH);
            document.Add(paymentTable);

            document.Close();
            return output.ToArray();
        }

        private static Table BuildTotalsTable(
            PdfFont font, PdfFont fontBold, CultureInfo culture,
            decimal subTotal, decimal freight, decimal tax, decimal invoiceTotal)
        {
            // Columnas: label (108pt), "US$" (32pt), monto (60pt) = 200pt total
            var table = new Table(UnitValue.CreatePointArray(new float[] { 108f, 32f, 60f }))
                .SetBorder(Border.NO_BORDER);

            var rows = new (string Label, decimal Amount, bool IsTotal)[]
            {
                ("SUB TOTAL",       subTotal,      false),
                ("FREIGHT",         freight,       false),
                ("TAXABLE AMOUNT",  tax,           false),
                ("TAX",             tax,           false),
                ("OTHER CHARGES",   0m,            false),
                ("INVOICE TOTAL",   invoiceTotal,  true),
            };

            for (int r = 0; r < rows.Length; r++)
            {
                var (label, amount, isTotal) = rows[r];
                bool first = r == 0;
                bool last  = r == rows.Length - 1;

                Border top    = first ? OuterBorder : Border.NO_BORDER;
                Border bottom = last  ? OuterBorder : InnerBorder;
                PdfFont vFont = isTotal ? fontBold : font;
                Color?  rowBg = isTotal ? GrayBg   : null;

                // Col 0: Label — siempre gris + negrita
                var c0 = new Cell()
                    .SetFont(fontBold).SetFontSize(8)
                    .SetPaddingLeft(3).SetPaddingRight(3).SetPaddingTop(2).SetPaddingBottom(2)
                    .Add(new Paragraph(label).SetMargin(0))
                    .SetBackgroundColor(GrayBg)
                    .SetBorderTop(top).SetBorderBottom(bottom)
                    .SetBorderLeft(OuterBorder).SetBorderRight(InnerBorder);
                table.AddCell(c0);

                // Col 1: "US$"
                var c1 = new Cell()
                    .SetFont(vFont).SetFontSize(8)
                    .SetPaddingLeft(3).SetPaddingRight(3).SetPaddingTop(2).SetPaddingBottom(2)
                    .Add(new Paragraph("US$").SetMargin(0))
                    .SetBorderTop(top).SetBorderBottom(bottom)
                    .SetBorderLeft(Border.NO_BORDER).SetBorderRight(InnerBorder);
                if (rowBg != null) c1.SetBackgroundColor(rowBg);
                table.AddCell(c1);

                // Col 2: Monto (alineado a la derecha)
                var c2 = new Cell()
                    .SetFont(vFont).SetFontSize(8)
                    .SetPaddingLeft(3).SetPaddingRight(3).SetPaddingTop(2).SetPaddingBottom(2)
                    .Add(new Paragraph(amount.ToString("N2", culture))
                        .SetMargin(0).SetTextAlignment(TextAlignment.RIGHT))
                    .SetBorderTop(top).SetBorderBottom(bottom)
                    .SetBorderLeft(Border.NO_BORDER).SetBorderRight(OuterBorder);
                if (rowBg != null) c2.SetBackgroundColor(rowBg);
                table.AddCell(c2);
            }

            return table;
        }

        private static Table BuildPaymentTable(PdfFont font, PdfFont fontBold, FE_Invoice_Transaccion enc)
        {
            // Columnas: label (90pt), valor (210pt) = 300pt total
            var table = new Table(UnitValue.CreatePointArray(new float[] { 90f, 210f }))
                .SetBorder(Border.NO_BORDER);

            // Fila de encabezado — abarca las 2 columnas
            table.AddCell(new Cell(1, 2)
                .SetFont(fontBold).SetFontSize(8)
                .SetPaddingLeft(3).SetPaddingRight(3).SetPaddingTop(2).SetPaddingBottom(2)
                .Add(new Paragraph("PAYMENT INSTRUCTIONS:").SetMargin(0))
                .SetBorderTop(OuterBorder).SetBorderBottom(InnerBorder)
                .SetBorderLeft(OuterBorder).SetBorderRight(OuterBorder));

            var payRows = new (string Label, string Value)[]
            {
                ("ACCOUNT NAME:",    "B' LEATHER MANUFACTURING, INC."),
                ("ACCOUNT NUMBER:",  enc.ACCT ?? ""),
                ("BANK:",            "CITIBANK, N.A."),
                ("BANK ADDRESS:",    "111 WALL STREET. NEW YORK, NY USA 10043"),
                ("ROUTING:",         (enc.ABA ?? "").PadLeft(9, '0')),
                ("SWIFT:",           enc.SWIFT ?? ""),
            };

            for (int r = 0; r < payRows.Length; r++)
            {
                var (label, value) = payRows[r];
                bool isLast = r == payRows.Length - 1;
                Border bottom = isLast ? OuterBorder : Border.NO_BORDER;

                // Label (col 0)
                table.AddCell(new Cell()
                    .SetFont(fontBold).SetFontSize(8)
                    .SetPaddingLeft(3).SetPaddingRight(3).SetPaddingTop(2).SetPaddingBottom(2)
                    .Add(new Paragraph(label).SetMargin(0))
                    .SetBorderTop(Border.NO_BORDER).SetBorderBottom(bottom)
                    .SetBorderLeft(OuterBorder).SetBorderRight(InnerBorder));

                // Value (col 1)
                table.AddCell(new Cell()
                    .SetFont(font).SetFontSize(8)
                    .SetPaddingLeft(3).SetPaddingRight(3).SetPaddingTop(2).SetPaddingBottom(2)
                    .Add(new Paragraph(value).SetMargin(0))
                    .SetBorderTop(Border.NO_BORDER).SetBorderBottom(bottom)
                    .SetBorderLeft(Border.NO_BORDER).SetBorderRight(OuterBorder));
            }

            return table;
        }
    }
}
