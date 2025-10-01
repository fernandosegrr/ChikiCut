using ChikiCut.web.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.Common;

namespace ChikiCut.web.Services
{
    public interface IClienteService
    {
        Task<List<Cliente>> GetAllAsync();
        Task<Cliente?> GetByIdAsync(long id);
        Task AddAsync(Cliente cliente);
        Task UpdateAsync(Cliente cliente);
        Task DeleteAsync(long id);
        DbConnection GetDbConnection();
    }
}