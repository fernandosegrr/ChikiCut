using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChikiCut.web.Services
{
    public class GastoService : IGastoService
    {
        private readonly AppDbContext _context;
        public GastoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Gasto>> GetAllAsync()
        {
            return await _context.Gastos.OrderByDescending(g => g.Fecha).ToListAsync();
        }

        public async Task<Gasto?> GetByIdAsync(long id)
        {
            return await _context.Gastos.FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task AddAsync(Gasto gasto)
        {
            _context.Gastos.Add(gasto);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Gasto gasto)
        {
            _context.Gastos.Update(gasto);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var gasto = await _context.Gastos.FindAsync(id);
            if (gasto != null)
            {
                _context.Gastos.Remove(gasto);
                await _context.SaveChangesAsync();
            }
        }
    }
}
