using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InvoiceOcr.Model;
using InvoiceOcr.Repositories.BaseRepository;

namespace InvoiceOcr.Repositories.InvoiceRepository
{
    public interface IInvoiceRepository : IBaseRepository<Invoice>
    {
        Task<Invoice> GetInvoiceWithDetailsAsync(int id);
        Task<List<Invoice>> GetInvoiceByCustomerAsync(string customerName);
    }
}
