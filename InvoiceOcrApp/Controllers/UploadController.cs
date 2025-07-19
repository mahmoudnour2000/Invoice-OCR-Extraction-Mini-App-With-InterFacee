using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using InvoiceOcr.Services;

namespace InvoiceOcr.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        #region Fields and Constructor
        private readonly OcrService _ocrService;
        private readonly InvoiceService _invoiceService;
        private readonly ILogger<UploadController> _logger;

        public UploadController(OcrService ocrService, InvoiceService invoiceService, ILogger<UploadController> logger)
        {
            _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
            _invoiceService = invoiceService ?? throw new ArgumentNullException(nameof(invoiceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        #endregion

        #region File Upload and Processing
        /// <summary>
        /// Uploads an image or PDF file and extracts invoice data.
        /// </summary>
        /// <param name="file">The image or PDF file to process.</param>
        /// <returns>An object containing the created invoice ID.</returns>
        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            #region Input Validation
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("No file uploaded or file is empty.");
                return BadRequest("No file uploaded or file is empty.");
            }

            if (!file.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) &&
                !file.FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) &&
                !file.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
                !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Unsupported file type: {FileName}", file.FileName);
                return BadRequest("Only JPG, PNG, or PDF files are supported.");
            }
            #endregion

            #region File Processing
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");
            try
            {
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var invoiceDto = await _ocrService.ExtractInvoiceDataAsync(tempFilePath);
                var invoiceId = await _invoiceService.CreateInvoiceAsync(invoiceDto);
                _logger.LogInformation("Invoice created from uploaded file with ID {InvoiceId}", invoiceId);
                return Ok(new { InvoiceId = invoiceId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing uploaded file: {FileName}", file.FileName);
                return StatusCode(500, "An error occurred while processing the file.");
            }
            finally
            {
                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
            }
            #endregion
        }
        #endregion
    }
}