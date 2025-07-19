using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InvoiceOcr.DTOs;
using InvoiceOcr.DTOs.Mappers;
using InvoiceOcr.Model;
using InvoiceOcr.Repositories.InvoiceDetailRepository;
using InvoiceOcr.Repositories.InvoiceRepository;
using Microsoft.Extensions.Logging;

namespace InvoiceOcr.Services
{
    public class InvoiceService
    {
        #region Fields and Constructor
        private readonly InvoiceRepository _invoiceRepository;
        private readonly InvoiceDetailRepository _invoiceDetailRepository;
        private readonly ILogger<InvoiceService> _logger;

        public InvoiceService(InvoiceRepository invoiceRepository, InvoiceDetailRepository invoiceDetailRepository, ILogger<InvoiceService> logger)
        {
            _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
            _invoiceDetailRepository = invoiceDetailRepository ?? throw new ArgumentNullException(nameof(invoiceDetailRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        #endregion

        #region Invoice Creation and Retrieval
        public async Task<int> CreateInvoiceAsync(InvoiceDto invoiceDto)
        {
            if (invoiceDto == null)
            {
                throw new ArgumentNullException(nameof(invoiceDto));
            }

            var invoice = invoiceDto.ToEntity();
            _logger.LogInformation("Creating invoice with {DetailCount} details", invoice.Details.Count);
            await _invoiceRepository.AddAsync(invoice);
            _logger.LogInformation("Invoice created with ID {InvoiceId}", invoice.Id);
            return invoice.Id;
        }

        public async Task<InvoiceDto> GetInvoiceAsync(int id)
        {
            var invoice = await _invoiceRepository.GetInvoiceWithDetailsAsync(id);
            return invoice?.ToDto();
        }

        public async Task<List<InvoiceDto>> GetInvoicesByCustomerAsync(string customerName)
        {
            if (string.IsNullOrWhiteSpace(customerName))
            {
                throw new ArgumentException("Customer name cannot be empty.", nameof(customerName));
            }

            var invoices = await _invoiceRepository.GetInvoiceByCustomerAsync(customerName);
            return invoices.ToDtoList();
        }
        #endregion

        #region Invoice Details Management
        public async Task<List<InvoiceDetailDto>> GetDetailsByInvoiceIdAsync(int invoiceId)
        {
            var details = await _invoiceDetailRepository.GetDetailsByInvoiceIdAsync(invoiceId);
            return details.ToDtoList();
        }
        #endregion

        #region Invoice Update Operations
        public async Task UpdateInvoiceAsync(InvoiceDto invoiceDto)
        {
            if (invoiceDto == null)
            {
                throw new ArgumentNullException(nameof(invoiceDto));
            }

            var invoice = await _invoiceRepository.GetInvoiceWithDetailsAsync(invoiceDto.Id);
            if (invoice == null)
            {
                throw new KeyNotFoundException($"Invoice with ID {invoiceDto.Id} not found.");
            }

            var updatedInvoice = invoiceDto.ToEntity();
            UpdateInvoiceProperties(invoice, updatedInvoice);
            await UpdateInvoiceDetails(invoice, updatedInvoice);

            await _invoiceRepository.UpdateAsync(invoice);
            _logger.LogInformation("Invoice with ID {InvoiceId} updated successfully", invoice.Id);
        }

        private void UpdateInvoiceProperties(Invoice invoice, Invoice updatedInvoice)
        {
            invoice.InvoiceNumber = updatedInvoice.InvoiceNumber;
            invoice.InvoiceDate = updatedInvoice.InvoiceDate;
            invoice.CustomerName = updatedInvoice.CustomerName;
            invoice.TotalAmount = updatedInvoice.TotalAmount;
            invoice.Vat = updatedInvoice.Vat;
        }

        private async Task UpdateInvoiceDetails(Invoice invoice, Invoice updatedInvoice)
        {
            var existingDetailIds = invoice.Details.Select(d => d.Id).ToList();
            var newDetailIds = updatedInvoice.Details.Where(d => d.Id != 0).Select(d => d.Id).ToList();

            var detailsToRemove = invoice.Details.Where(d => !newDetailIds.Contains(d.Id)).ToList();
            foreach (var detail in detailsToRemove)
            {
                invoice.Details.Remove(detail);
                await _invoiceDetailRepository.DeleteAsync(detail.Id);
            }

            foreach (var updatedDetail in updatedInvoice.Details)
            {
                var existingDetail = invoice.Details.FirstOrDefault(d => d.Id == updatedDetail.Id && updatedDetail.Id != 0);
                if (existingDetail != null)
                {
                    existingDetail.Description = updatedDetail.Description;
                    existingDetail.Quantity = updatedDetail.Quantity;
                    existingDetail.UnitPrice = updatedDetail.UnitPrice;
                    existingDetail.LineTotal = updatedDetail.LineTotal;
                }
                else
                {
                    invoice.Details.Add(updatedDetail);
                }
            }
        }
        #endregion

        #region Get All Invoices
        public async Task<List<InvoiceDto>> GetAllInvoicesAsync()
        {
            var invoices = await _invoiceRepository.GetAllAsync();
            return invoices.ToDtoList();
        }
        #endregion
    }
}
