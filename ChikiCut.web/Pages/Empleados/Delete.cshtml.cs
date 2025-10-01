using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;

namespace ChikiCut.web.Pages.Empleados
{
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;

        public DeleteModel(AppDbContext context) => _context = context;

        [BindProperty]
        public Empleado Empleado { get; set; } = default!;

        public bool TieneRelaciones { get; set; } = false;
        public string MensajeRelaciones { get; set; } = string.Empty;
        public string TipoOperacion { get; set; } = "desactivar"; // "desactivar" o "reactivar"

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var empleado = await _context.Empleados
                .Include(e => e.Sucursal)
                .Include(e => e.PuestoNavegacion)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (empleado == null)
            {
                return NotFound();
            }

            Empleado = empleado;

            // Determinar la operaci�n a realizar
            TipoOperacion = empleado.IsActive ? "desactivar" : "reactivar";

            // Verificar si es el �nico empleado activo de la sucursal (solo si se va a desactivar)
            if (empleado.IsActive)
            {
                var empleadosActivosEnSucursal = await _context.Empleados
                    .Where(e => e.SucursalId == empleado.SucursalId && e.IsActive && e.Id != empleado.Id)
                    .CountAsync();

                if (empleadosActivosEnSucursal == 0)
                {
                    TieneRelaciones = true;
                    MensajeRelaciones = "No se puede desactivar este empleado porque es el �nico empleado activo en esta sucursal.";
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var empleado = await _context.Empleados
                .Include(e => e.Sucursal)
                .Include(e => e.PuestoNavegacion)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (empleado != null)
            {
                // Verificaci�n adicional antes de desactivar
                if (empleado.IsActive)
                {
                    var empleadosActivosEnSucursal = await _context.Empleados
                        .Where(e => e.SucursalId == empleado.SucursalId && e.IsActive && e.Id != empleado.Id)
                        .CountAsync();

                    if (empleadosActivosEnSucursal == 0)
                    {
                        ModelState.AddModelError(string.Empty, "No se puede desactivar este empleado porque es el �nico empleado activo en esta sucursal.");
                        Empleado = empleado;
                        TieneRelaciones = true;
                        MensajeRelaciones = "No se puede desactivar este empleado porque es el �nico empleado activo en esta sucursal.";
                        TipoOperacion = "desactivar";
                        return Page();
                    }
                }

                try
                {
                    // ELIMINACI�N L�GICA - cambiar estado
                    empleado.IsActive = !empleado.IsActive;
                    
                    // Si se est� desactivando, registrar fecha de baja
                    if (!empleado.IsActive)
                    {
                        empleado.FechaBaja = DateOnly.FromDateTime(DateTime.Now);
                    }
                    else
                    {
                        // Si se est� reactivando, limpiar fecha de baja
                        empleado.FechaBaja = null;
                    }
                    
                    empleado.UpdatedAt = DateTime.UtcNow;
                    
                    _context.Update(empleado);
                    await _context.SaveChangesAsync();

                    // Mensaje de confirmaci�n seg�n la acci�n
                    TempData["SuccessMessage"] = empleado.IsActive ? 
                        $"Empleado {empleado.NombreCompleto} reactivado exitosamente." :
                        $"Empleado {empleado.NombreCompleto} desactivado exitosamente.";
                }
                catch (Exception ex)
                {
                    var accion = empleado.IsActive ? "reactivar" : "desactivar";
                    ModelState.AddModelError(string.Empty, $"Error al {accion} el empleado: {ex.Message}");
                    Empleado = empleado;
                    TipoOperacion = empleado.IsActive ? "desactivar" : "reactivar";
                    return Page();
                }
            }

            return RedirectToPage("./Index");
        }
    }
}