using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Extensions;

namespace ChikiCut.web.Pages.ConceptosGasto
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;

        public DetailsModel(AppDbContext context)
        {
            _context = context;
        }

        public ConceptoGasto ConceptoGasto { get; set; } = default!;
        public List<Sucursal> SucursalesAplicables { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var conceptoGasto = await _context.ConceptosGasto
                .Include(c => c.UsuarioCreador)
                    .ThenInclude(u => u.Empleado)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (conceptoGasto == null)
            {
                return NotFound();
            }

            ConceptoGasto = conceptoGasto;

            // Cargar sucursales específicas si aplica
            if (!ConceptoGasto.AplicaTodasSucursales && ConceptoGasto.SucursalesAplicablesList.Any())
            {
                SucursalesAplicables = await _context.Sucursals
                    .Where(s => ConceptoGasto.SucursalesAplicablesList.Contains(s.Id))
                    .OrderBy(s => s.Name)
                    .ToListAsync();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostActivateAsync(long id)
        {
            var concepto = await _context.ConceptosGasto.FindAsync(id);
            if (concepto == null)
            {
                return NotFound();
            }

            concepto.IsActive = true;
            concepto.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesUtcAsync();

            return new JsonResult(new { success = true });
        }
    }
}