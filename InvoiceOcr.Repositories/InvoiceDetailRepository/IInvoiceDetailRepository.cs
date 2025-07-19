using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InvoiceOcr.Model;
using InvoiceOcr.Repositories.BaseRepository;

namespace InvoiceOcr.Repositories.InvoiceDetailRepository
{
    public interface IInvoiceDetailRepository : IBaseRepository<InvoiceDetail>
    {
        Task<List<InvoiceDetail>> GetDetailsByInvoiceIdAsync(int invoiceId);
    }
}
