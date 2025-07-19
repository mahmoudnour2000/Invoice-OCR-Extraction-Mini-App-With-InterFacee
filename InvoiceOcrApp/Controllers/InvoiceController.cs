using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using InvoiceOcr.DTOs;
using InvoiceOcr.Services;

namespace InvoiceOcr.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        #region Fields and Constructor
        private readonly InvoiceService _invoiceService;
        private readonly ILogger<InvoiceController> _logger;

        public InvoiceController(InvoiceService invoiceService, ILogger<InvoiceController> logger)
        {
            _invoiceService = invoiceService ?? throw new ArgumentNullException(nameof(invoiceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        #endregion

        #region Invoice Creation
        [HttpPost]
        public async Task<IActionResult> CreateInvoice([FromBody] InvoiceDto invoiceDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for CreateInvoice request.");
                return BadRequest(ModelState);
            }

            try
            {
                var invoiceId = await _invoiceService.CreateInvoiceAsync(invoiceDto);
                _logger.LogInformation("Invoice created successfully with ID {InvoiceId}.", invoiceId);
                return Ok(new { InvoiceId = invoiceId });
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning(ex, "Invalid invoice data provided for CreateInvoice.");
                return BadRequest("Invoice data cannot be null.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice.");
                return StatusCode(500, "An error occurred while creating the invoice.");
            }
        }
        #endregion

        #region Invoice Retrieval
        [HttpGet("{id}")]
        public async Task<IActionResult> GetInvoice(int id)
        {
            try
            {
                var invoice = await _invoiceService.GetInvoiceAsync(id);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice with ID {InvoiceId} not found.", id);
                    return NotFound();
                }

                return Ok(invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice with ID {InvoiceId}.", id);
                return StatusCode(500, "An error occurred while retrieving the invoice.");
            }
        }

        [HttpGet("customer/{customerName}")]
        public async Task<IActionResult> GetInvoicesByCustomer(string customerName)
        {
            try
            {
                var invoices = await _invoiceService.GetInvoicesByCustomerAsync(customerName);
                return Ok(invoices);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid customer name provided: {CustomerName}.", customerName);
                return BadRequest("Customer name cannot be empty.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices for customer {CustomerName}.", customerName);
                return StatusCode(500, "An error occurred while retrieving invoices.");
            }
        }

        [HttpGet("details/{invoiceId}")]
        public async Task<IActionResult> GetDetailsByInvoiceId(int invoiceId)
        {
            try
            {
                var details = await _invoiceService.GetDetailsByInvoiceIdAsync(invoiceId);
                return Ok(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving details for invoice ID {InvoiceId}.", invoiceId);
                return StatusCode(500, "An error occurred while retrieving invoice details.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllInvoices()
        {
            try
            {
                var invoices = await _invoiceService.GetAllInvoicesAsync();
                _logger.LogInformation("Retrieved {Count} invoices.", invoices.Count);
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all invoices.");
                return StatusCode(500, "An error occurred while retrieving invoices.");
            }
        }
        #endregion

        #region Invoice Update
        [HttpPut]
        public async Task<IActionResult> UpdateInvoice([FromBody] InvoiceDto invoiceDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for UpdateInvoice request.");
                return BadRequest(ModelState);
            }

            try
            {
                await _invoiceService.UpdateInvoiceAsync(invoiceDto);
                _logger.LogInformation("Invoice with ID {InvoiceId} updated successfully.", invoiceDto.Id);
                return Ok();
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning(ex, "Invalid invoice data provided for UpdateInvoice.");
                return BadRequest("Invoice data cannot be null.");
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Invoice with ID {InvoiceId} not found for update.", invoiceDto.Id);
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice with ID {InvoiceId}.", invoiceDto.Id);
                return StatusCode(500, "An error occurred while updating the invoice.");
            }
        }
        #endregion
    }
}
