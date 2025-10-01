using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;

namespace ChikiCut.web.Pages.Puestos
{
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;

        public DeleteModel(AppDbContext context) => _context = context;

        [BindProperty]
        public Puesto Puesto { get; set; } = default!;

        public bool TieneRelaciones { get; set; } = false;
        public string MensajeRelaciones { get; set; } = string.Empty;
        public string TipoOperacion { get; set; } = "desactivar"; // "desactivar" o "reactivar"
        public int EmpleadosAsignados { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var puesto = await _context.Puestos
                .Include(p => p.Empleados.Where(e => e.IsActive))
                .FirstOrDefaultAsync(m => m.Id == id);

            if (puesto == null)
            {
                return NotFound();
            }

            Puesto = puesto;
            EmpleadosAsignados = puesto.Empleados.Count;

            // Determinar la operación a realizar
            TipoOperacion = puesto.IsActive ? "desactivar" : "reactivar";

            // Verificar si tiene empleados asignados (solo si se va a desactivar)
            if (puesto.IsActive && EmpleadosAsignados > 0)
            {
                TieneRelaciones = true;
                MensajeRelaciones = $"No se puede desactivar este puesto porque tiene {EmpleadosAsignados} empleado(s) asignado(s). " +
                                   "Debe reasignar o desactivar a todos los empleados antes de desactivar el puesto.";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var puesto = await _context.Puestos
                .Include(p => p.Empleados.Where(e => e.IsActive))
                .FirstOrDefaultAsync(p => p.Id == id);

            if (puesto != null)
            {
                // Verificación adicional antes de desactivar
                if (puesto.IsActive && puesto.Empleados.Any())
                {
                    ModelState.AddModelError(string.Empty, 
                        $"No se puede desactivar este puesto porque tiene {puesto.Empleados.Count} empleado(s) asignado(s).");
                    
                    Puesto = puesto;
                    EmpleadosAsignados = puesto.Empleados.Count;
                    TieneRelaciones = true;
                    MensajeRelaciones = $"No se puede desactivar este puesto porque tiene {puesto.Empleados.Count} empleado(s) asignado(s).";
                    TipoOperacion = "desactivar";
                    return Page();
                }

                try
                {
                    // ELIMINACIÓN LÓGICA - cambiar estado
                    puesto.IsActive = !puesto.IsActive;
                    puesto.UpdatedAt = DateTime.UtcNow;
                    
                    _context.Update(puesto);
                    await _context.SaveChangesAsync();

                    // Mensaje de confirmación según la acción
                    TempData["SuccessMessage"] = puesto.IsActive ? 
                        $"Puesto '{puesto.Nombre}' reactivado exitosamente." :
                        $"Puesto '{puesto.Nombre}' desactivado exitosamente.";
                }
                catch (Exception ex)
                {
                    var accion = puesto.IsActive ? "reactivar" : "desactivar";
                    ModelState.AddModelError(string.Empty, $"Error al {accion} el puesto: {ex.Message}");
                    Puesto = puesto;
                    EmpleadosAsignados = puesto.Empleados.Count;
                    TipoOperacion = puesto.IsActive ? "desactivar" : "reactivar";
                    return Page();
                }
            }

            return RedirectToPage("./Index");
        }
    }
}