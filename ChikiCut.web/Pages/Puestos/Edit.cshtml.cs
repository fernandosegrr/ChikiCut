using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;

namespace ChikiCut.web.Pages.Puestos
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditModel(AppDbContext context) => _context = context;

        [BindProperty]
        public Puesto Puesto { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var puesto = await _context.Puestos.FirstOrDefaultAsync(m => m.Id == id);
            if (puesto == null)
            {
                return NotFound();
            }
            Puesto = puesto;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Remover validaciones de campos automáticos
            ModelState.Remove("Puesto.CreatedAt");
            ModelState.Remove("Puesto.UpdatedAt");
            ModelState.Remove("Puesto.Empleados");

            // Validación personalizada de rangos salariales
            if (Puesto.SalarioBaseMinimo.HasValue && Puesto.SalarioBaseMaximo.HasValue)
            {
                if (Puesto.SalarioBaseMinimo > Puesto.SalarioBaseMaximo)
                {
                    ModelState.AddModelError("Puesto.SalarioBaseMaximo", "El salario máximo debe ser mayor o igual al salario mínimo.");
                }
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var entity = await _context.Puestos.FindAsync(Puesto.Id);
                if (entity == null)
                {
                    return NotFound();
                }

                // Mapear campos editables
                entity.Nombre = Puesto.Nombre;
                entity.Descripcion = Puesto.Descripcion;
                entity.SalarioBaseMinimo = Puesto.SalarioBaseMinimo;
                entity.SalarioBaseMaximo = Puesto.SalarioBaseMaximo;
                entity.RequiereExperiencia = Puesto.RequiereExperiencia;
                entity.NivelJerarquico = Puesto.NivelJerarquico;
                entity.IsActive = Puesto.IsActive;
                entity.UpdatedAt = DateTime.UtcNow;

                _context.Update(entity);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Puesto '{Puesto.Nombre}' actualizado exitosamente.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error al actualizar el puesto: {ex.Message}");
                return Page();
            }
        }
    }
}