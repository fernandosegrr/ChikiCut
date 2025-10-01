using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using System.Text.Json;

namespace ChikiCut.web.Pages.Empleados
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;

        public DetailsModel(AppDbContext context) => _context = context;

        public Empleado? Empleado { get; set; }
        public List<string> EspecialidadesList { get; set; } = new();
        public Dictionary<string, object> HorarioTrabajoDict { get; set; } = new();
        public Dictionary<string, object> ComisionesDict { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound("ID de empleado no proporcionado");
            }

            try
            {
                Empleado = await _context.Empleados
                    .Include(e => e.Sucursal)
                    .Include(e => e.PuestoNavegacion)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (Empleado == null)
                {
                    return NotFound($"Empleado con ID {id} no encontrado");
                }

                // Parsear los campos JSON para mostrar de forma legible
                try
                {
                    if (!string.IsNullOrEmpty(Empleado.Especialidades))
                    {
                        EspecialidadesList = JsonSerializer.Deserialize<List<string>>(Empleado.Especialidades) ?? new List<string>();
                    }
                }
                catch (JsonException)
                {
                    EspecialidadesList = new List<string> { "Error al cargar especialidades" };
                }

                try
                {
                    if (!string.IsNullOrEmpty(Empleado.HorarioTrabajo))
                    {
                        HorarioTrabajoDict = JsonSerializer.Deserialize<Dictionary<string, object>>(Empleado.HorarioTrabajo) ?? new Dictionary<string, object>();
                    }
                }
                catch (JsonException)
                {
                    HorarioTrabajoDict = new Dictionary<string, object> { { "error", "Error al cargar horario" } };
                }

                try
                {
                    if (!string.IsNullOrEmpty(Empleado.Comisiones))
                    {
                        ComisionesDict = JsonSerializer.Deserialize<Dictionary<string, object>>(Empleado.Comisiones) ?? new Dictionary<string, object>();
                    }
                }
                catch (JsonException)
                {
                    ComisionesDict = new Dictionary<string, object> { { "error", "Error al cargar comisiones" } };
                }

                return Page();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al cargar los detalles del empleado: {ex.Message}");
            }
        }
    }
}