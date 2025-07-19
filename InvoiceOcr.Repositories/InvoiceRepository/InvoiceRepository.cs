using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InvoiceOcr.Data;
using InvoiceOcr.Model;
using InvoiceOcr.Repositories.BaseRepository;
using iText.Commons.Actions.Contexts;

namespace InvoiceOcr.Repositories.InvoiceRepository
{
    public class InvoiceRepository : IInvoiceRepository
    {
        #region Constructor
        private readonly AppDbContext _context;

        public InvoiceRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
        #endregion

        #region Query Operations
        public async Task<Invoice> GetInvoiceWithDetailsAsync(int id)
        {
            return await _context.Invoices
                .Include(i => i.Details)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<List<Invoice>> GetInvoiceByCustomerAsync(string customerName)
        {
            if (string.IsNullOrWhiteSpace(customerName))
            {
                throw new ArgumentException("Customer name cannot be empty.", nameof(customerName));
            }

            return await _context.Invoices
                .Include(i => i.Details)
                .Where(i => i.CustomerName.Contains(customerName))
                .ToListAsync();
        }
        #endregion

        #region IBaseRepository Implementation
        public async Task<Invoice> GetByIdAsync(int id)
        {
            return await _context.Set<Invoice>().FindAsync(id);
        }

        public async Task<List<Invoice>> GetAllAsync()
        {
            return await _context.Set<Invoice>().ToListAsync();
        }

        public async Task AddAsync(Invoice entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            await _context.Set<Invoice>().AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Invoice entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _context.Set<Invoice>().Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"Entity with ID {id} not found.");
            }
            _context.Set<Invoice>().Remove(entity);
            await _context.SaveChangesAsync();
        }
        #endregion
    }
}