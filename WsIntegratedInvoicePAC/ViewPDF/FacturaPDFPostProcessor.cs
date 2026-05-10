using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;

namespace WsIntegratedInvoicePAC.ViewPDF
{
    public static class FacturaPDFPostProcessor
    {
        // Puntos desde el fondo absoluto de la página que cubre: margen inferior (20pt) + contenido del footer (~218pt) + buffer (2pt)
        private const float FooterCoverHeight = 240f;

        public static byte[] HideFooterOnNonLastPages(byte[] pdfBytes)
        {
            var ms = new MemoryStream();

            using (var reader = new PdfReader(new MemoryStream(pdfBytes)))
            using (var writer = new PdfWriter(ms))
            using (var doc = new PdfDocument(reader, writer))
            {
                int totalPages = doc.GetNumberOfPages();

                for (int i = 1; i < totalPages; i++) // páginas 1..N-1, se salta la última
                {
                    var page = doc.GetPage(i);
                    float pageWidth = page.GetPageSize().GetWidth();

                    var cv = new PdfCanvas(page);
                    cv.SaveState();
                    cv.SetFillColor(ColorConstants.WHITE);
                    cv.Rectangle(0, 0, pageWidth, FooterCoverHeight);
                    cv.Fill();
                    cv.RestoreState();
                    cv.Release();
                }
            } // doc y writer se cierran aquí → el PDF queda completamente escrito en ms

            return ms.ToArray();
        }
    }
}
