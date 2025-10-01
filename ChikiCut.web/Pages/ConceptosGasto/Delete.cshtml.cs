using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Extensions;

namespace ChikiCut.web.Pages.ConceptosGasto
{
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;

        public DeleteModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ConceptoGasto ConceptoGasto { get; set; } = default!;
        
        public bool TieneGastosAsociados { get; set; }
        public string TipoOperacion { get; set; } = "desactivar";

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var conceptoGasto = await _context.ConceptosGasto
                .Include(c => c.UsuarioCreador)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (conceptoGasto == null)
            {
                return NotFound();
            }

            ConceptoGasto = conceptoGasto;
            
            // TODO: Verificar si tiene gastos asociados cuando se implemente el módulo de gastos
            TieneGastosAsociados = false;
            
            // Determinar tipo de operación
            TipoOperacion = ConceptoGasto.IsActive ? "desactivar" : "activar";

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var conceptoGasto = await _context.ConceptosGasto.FindAsync(id);
            if (conceptoGasto == null)
            {
                return NotFound();
            }

            // En lugar de eliminar, desactivamos el concepto
            conceptoGasto.IsActive = !conceptoGasto.IsActive;
            conceptoGasto.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesUtcAsync();

            return RedirectToPage("./Index");
        }
    }
}