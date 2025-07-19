using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InvoiceOcr.Data;
using InvoiceOcr.Model;
using InvoiceOcr.Repositories.BaseRepository;

namespace InvoiceOcr.Repositories.InvoiceDetailRepository
{
    public class InvoiceDetailRepository : IInvoiceDetailRepository
    {
        #region Constructor
        private readonly AppDbContext _context;

        public InvoiceDetailRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
        #endregion

        #region Query Operations
        public async Task<List<InvoiceDetail>> GetDetailsByInvoiceIdAsync(int invoiceId)
        {
            return await _context.InvoiceDetails
                .Where(d => d.InvoiceId == invoiceId)
                .ToListAsync();
        }
        #endregion

        #region IBaseRepository Implementation
        public async Task<InvoiceDetail> GetByIdAsync(int id)
        {
            return await _context.Set<InvoiceDetail>().FindAsync(id);
        }

        public async Task<List<InvoiceDetail>> GetAllAsync()
        {
            return await _context.Set<InvoiceDetail>().ToListAsync();
        }

        public async Task AddAsync(InvoiceDetail entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            await _context.Set<InvoiceDetail>().AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(InvoiceDetail entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _context.Set<InvoiceDetail>().Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"Entity with ID {id} not found.");
            }
            _context.Set<InvoiceDetail>().Remove(entity);
            await _context.SaveChangesAsync();
        }
        #endregion
    }
}