using System.Threading.Tasks;
using System.Collections.Generic;
using ChikiCut.web.Data.Entities;

namespace ChikiCut.web.Services
{
    public interface IGastoService
    {
        Task<List<Gasto>> GetAllAsync();
        Task<Gasto?> GetByIdAsync(long id);
        Task AddAsync(Gasto gasto);
        Task UpdateAsync(Gasto gasto);
        Task DeleteAsync(long id);
    }
}
