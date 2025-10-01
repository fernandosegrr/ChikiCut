using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Services;
using System.Text.Json;

namespace ChikiCut.web.Pages.Empleados
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ISucursalFilterService _sucursalFilter;

        public EditModel(AppDbContext context, ISucursalFilterService sucursalFilter)
        {
            _context = context;
            _sucursalFilter = sucursalFilter;
        }

        [BindProperty]
        public Empleado Empleado { get; set; } = default!;

        [BindProperty]
        public List<string> EspecialidadesSeleccionadas { get; set; } = new();

        public SelectList SucursalOptions { get; set; } = default!;
        public SelectList PuestoOptions { get; set; } = default!;
        public bool TieneAccesoGlobal { get; set; }
        public List<string> SucursalesUsuario { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Empleado = await _context.Empleados.FirstOrDefaultAsync(m => m.Id == id);

            if (Empleado == null)
            {
                return NotFound();
            }

            // VALIDAR ACCESO A LA SUCURSAL DEL EMPLEADO
            var userId = HttpContext.Session.GetString("UserId");
            if (long.TryParse(userId, out var usuarioId))
            {
                var tieneAcceso = await _sucursalFilter.UsuarioTieneAccesoSucursalAsync(usuarioId, Empleado.SucursalId);
                if (!tieneAcceso)
                {
                    return RedirectToPage("/AccessDenied");
                }
            }

            await LoadSucursalesAsync();
            await LoadPuestosAsync();

            // Cargar especialidades existentes para mostrar en checkboxes
            try
            {
                if (!string.IsNullOrEmpty(Empleado.Especialidades))
                {
                    EspecialidadesSeleccionadas = JsonSerializer.Deserialize<List<string>>(Empleado.Especialidades) ?? new List<string>();
                }
            }
            catch (JsonException)
            {
                EspecialidadesSeleccionadas = new List<string>();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // VALIDAR ACCESO A LA SUCURSAL ORIGINAL Y NUEVA
            var userId = HttpContext.Session.GetString("UserId");
            if (long.TryParse(userId, out var usuarioId))
            {
                // Obtener empleado original para verificar acceso
                var empleadoOriginal = await _context.Empleados
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == Empleado.Id);

                if (empleadoOriginal != null)
                {
                    var tieneAccesoOriginal = await _sucursalFilter.UsuarioTieneAccesoSucursalAsync(usuarioId, empleadoOriginal.SucursalId);
                    if (!tieneAccesoOriginal)
                    {
                        return RedirectToPage("/AccessDenied");
                    }
                }

                // Validar acceso a la nueva sucursal si cambió
                var tieneAccesoNueva = await _sucursalFilter.UsuarioTieneAccesoSucursalAsync(usuarioId, Empleado.SucursalId);
                if (!tieneAccesoNueva)
                {
                    ModelState.AddModelError("Empleado.SucursalId", "No tienes permisos para asignar empleados a esta sucursal.");
                }
            }

            await LoadSucursalesAsync();
            await LoadPuestosAsync();

            // Remover validaciones de campos automáticos
            ModelState.Remove("Empleado.CreatedAt");
            ModelState.Remove("Empleado.UpdatedAt");
            ModelState.Remove("Empleado.Sucursal");
            ModelState.Remove("Empleado.PuestoNavegacion");

            // Configurar valores predefinidos si están vacíos
            if (string.IsNullOrEmpty(Empleado.Pais))
                Empleado.Pais = "MX";

            if (string.IsNullOrEmpty(Empleado.Especialidades))
                Empleado.Especialidades = "[]";

            if (string.IsNullOrEmpty(Empleado.HorarioTrabajo))
                Empleado.HorarioTrabajo = "{\"lun\":[{\"inicio\":\"09:00\",\"fin\":\"18:00\"}],\"mar\":[{\"inicio\":\"09:00\",\"fin\":\"18:00\"}],\"mie\":[{\"inicio\":\"09:00\",\"fin\":\"18:00\"}],\"jue\":[{\"inicio\":\"09:00\",\"fin\":\"18:00\"}],\"vie\":[{\"inicio\":\"09:00\",\"fin\":\"18:00\"}],\"sab\":[{\"inicio\":\"09:00\",\"fin\":\"14:00\"}]}";

            if (string.IsNullOrEmpty(Empleado.Comisiones))
                Empleado.Comisiones = "{\"porcentaje_servicios\":10}";

            // Procesar especialidades seleccionadas
            if (EspecialidadesSeleccionadas?.Any() == true)
            {
                Empleado.Especialidades = JsonSerializer.Serialize(EspecialidadesSeleccionadas);
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var entity = await _context.Empleados.FindAsync(Empleado.Id);
                if (entity == null)
                {
                    return NotFound();
                }

                // Mapear campos editables
                entity.SucursalId = Empleado.SucursalId;
                entity.PuestoId = Empleado.PuestoId;
                entity.CodigoEmpleado = Empleado.CodigoEmpleado;
                entity.Nombre = Empleado.Nombre;
                entity.ApellidoPaterno = Empleado.ApellidoPaterno;
                entity.ApellidoMaterno = Empleado.ApellidoMaterno;
                entity.Alias = Empleado.Alias;
                entity.Telefono = Empleado.Telefono;
                entity.TelefonoEmergencia = Empleado.TelefonoEmergencia;
                entity.Email = Empleado.Email;
                entity.FechaNacimiento = Empleado.FechaNacimiento;
                entity.Curp = Empleado.Curp;
                entity.Rfc = Empleado.Rfc;
                entity.Nss = Empleado.Nss;
                entity.SalarioBase = Empleado.SalarioBase;
                entity.FechaIngreso = Empleado.FechaIngreso;
                entity.FechaBaja = Empleado.FechaBaja;
                entity.DireccionLinea1 = Empleado.DireccionLinea1;
                entity.DireccionLinea2 = Empleado.DireccionLinea2;
                entity.Ciudad = Empleado.Ciudad;
                entity.Estado = Empleado.Estado;
                entity.CodigoPostal = Empleado.CodigoPostal;
                entity.ContactoEmergenciaNombre = Empleado.ContactoEmergenciaNombre;
                entity.ContactoEmergenciaTelefono = Empleado.ContactoEmergenciaTelefono;
                entity.ContactoEmergenciaParentesco = Empleado.ContactoEmergenciaParentesco;
                entity.Especialidades = Empleado.Especialidades;
                entity.HorarioTrabajo = Empleado.HorarioTrabajo;
                entity.Comisiones = Empleado.Comisiones;
                entity.IsActive = Empleado.IsActive;

                // Campos que no deben cambiar
                if (string.IsNullOrEmpty(entity.Pais))
                    entity.Pais = "MX";

                // Campo de sistema
                entity.UpdatedAt = DateTime.UtcNow;

                _context.Update(entity);
                await _context.SaveChangesAsync();

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error al guardar los cambios: {ex.Message}");
                return Page();
            }
        }

        private async Task LoadSucursalesAsync()
        {
            // Obtener ID del usuario actual
            var userId = HttpContext.Session.GetString("UserId");
            if (!long.TryParse(userId, out var usuarioId))
            {
                SucursalOptions = new SelectList(new List<object>(), "Id", "Name");
                return;
            }

            // Verificar si tiene acceso global
            TieneAccesoGlobal = await _sucursalFilter.TieneAccesoGlobalAsync(usuarioId);

            List<object> sucursales;

            if (TieneAccesoGlobal)
            {
                // Si tiene acceso global, mostrar todas las sucursales
                sucursales = await _context.Sucursals
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Name)
                    .Select(s => new { s.Id, s.Name })
                    .Cast<object>()
                    .ToListAsync();
            }
            else
            {
                // Solo mostrar sucursales a las que tiene acceso
                var sucursalesIds = await _sucursalFilter.GetSucursalesUsuarioAsync(usuarioId);
                sucursales = await _context.Sucursals
                    .Where(s => s.IsActive && sucursalesIds.Contains(s.Id))
                    .OrderBy(s => s.Name)
                    .Select(s => new { s.Id, s.Name })
                    .Cast<object>()
                    .ToListAsync();

                // Obtener nombres para mostrar en la interfaz
                SucursalesUsuario = await _context.Sucursals
                    .Where(s => sucursalesIds.Contains(s.Id))
                    .Select(s => s.Name)
                    .OrderBy(name => name)
                    .ToListAsync();
            }

            SucursalOptions = new SelectList(sucursales, "Id", "Name");
        }

        private async Task LoadPuestosAsync()
        {
            var puestos = await _context.Puestos
                .Where(p => p.IsActive)
                .OrderBy(p => p.Nombre)
                .Select(p => new { p.Id, p.Nombre })
                .ToListAsync();

            PuestoOptions = new SelectList(puestos, "Id", "Nombre");
        }
    }
}