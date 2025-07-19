using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceOcr.Repositories.BaseRepository
{
    public interface IBaseRepository<T> where T : class
    {
        #region Query Operations
        Task<T> GetByIdAsync(int id);
        Task<List<T>> GetAllAsync();
        #endregion

        #region Command Operations
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);
        #endregion
    }
}
