using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using System.Drawing;
using System.Drawing.Imaging;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace InvoiceOcr.Services
{
    public class PdfConverter
    {
        #region PDF to Image Conversion
        public async Task<Stream> ConvertPdfToImageAsync(string pdfPath)
        {
            if (!File.Exists(pdfPath))
            {
                throw new FileNotFoundException($"PDF file not found: {pdfPath}");
            }

            try
            {
                return await Task.Run(() =>
                {
                    #region PDF Document Setup
                    using var pdfReader = new PdfReader(pdfPath);
                    using var pdfDocument = new PdfDocument(pdfReader);
                    
                    if (pdfDocument.GetNumberOfPages() == 0)
                    {
                        throw new InvalidOperationException("PDF document has no pages.");
                    }

                    var page = pdfDocument.GetFirstPage();
                    var pageSize = page.GetPageSize();
                    #endregion

                    #region Image Generation Setup
                    var width = (int)(pageSize.GetWidth() * 2);
                    var height = (int)(pageSize.GetHeight() * 2);
                    
                    using var bitmap = new Bitmap(width, height);
                    using var graphics = Graphics.FromImage(bitmap);
                    
                    graphics.Clear(System.Drawing.Color.White);
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    #endregion

                    #region Text Extraction and Rendering
                    var strategy = new SimpleTextExtractionStrategy();
                    var text = PdfTextExtractor.GetTextFromPage(page, strategy);
                    
                    using var font = new Font("Arial", 12);
                    using var brush = new SolidBrush(System.Drawing.Color.Black);
                    
                    var lines = text.Split('\n');
                    var y = 20;
                    
                    foreach (var line in lines)
                    {
                        graphics.DrawString(line, font, brush, 20, y);
                        y += 20;
                    }
                    #endregion

                    #region Image Stream Generation
                    var memoryStream = new MemoryStream();
                    bitmap.Save(memoryStream, ImageFormat.Png);
                    memoryStream.Position = 0;
                    
                    return memoryStream;
                    #endregion
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to convert PDF to image: {ex.Message}", ex);
            }
        }
        #endregion
    }
}
