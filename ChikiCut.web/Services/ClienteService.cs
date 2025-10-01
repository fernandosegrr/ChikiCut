using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace ChikiCut.web.Services
{
    public class ClienteService : IClienteService
    {
        private readonly AppDbContext _context;
        public ClienteService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Cliente>> GetAllAsync()
        {
            // Retorna todos los clientes, sin filtrar por IsActive
            return await _context.Clientes.OrderByDescending(c => c.CreatedAt).ToListAsync();
        }

        public async Task<Cliente?> GetByIdAsync(long id)
        {
            // Retorna el cliente por id, sin filtrar por IsActive
            return await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AddAsync(Cliente cliente)
        {
            cliente.IsActive = true;
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Cliente cliente)
        {
            _context.Clientes.Update(cliente);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente != null)
            {
                cliente.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }

        public DbConnection GetDbConnection()
        {
            return _context.Database.GetDbConnection();
        }
    }
}