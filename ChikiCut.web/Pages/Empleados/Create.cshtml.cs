using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Services;

namespace ChikiCut.web.Pages.Empleados
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ISucursalFilterService _sucursalFilter;

        public CreateModel(AppDbContext context, ISucursalFilterService sucursalFilter)
        {
            _context = context;
            _sucursalFilter = sucursalFilter;
        }

        public SelectList SucursalOptions { get; set; } = default!;
        public SelectList PuestoOptions { get; set; } = default!;
        public bool TieneAccesoGlobal { get; set; }
        public List<string> SucursalesUsuario { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadSucursalesAsync();
            await LoadPuestosAsync();

            // Inicializar empleado con valores predefinidos
            Empleado = new Empleado
            {
                Pais = "MX",
                IsActive = true,
                FechaIngreso = DateOnly.FromDateTime(DateTime.Now),
                Estado = "CDMX",
                Ciudad = "Ciudad de México",
                Especialidades = "[]", // Array JSON vacío
                HorarioTrabajo = "{\"lun\":[{\"inicio\":\"09:00\",\"fin\":\"18:00\"}],\"mar\":[{\"inicio\":\"09:00\",\"fin\":\"18:00\"}],\"mie\":[{\"inicio\":\"09:00\",\"fin\":\"18:00\"}],\"jue\":[{\"inicio\":\"09:00\",\"fin\":\"18:00\"}],\"vie\":[{\"inicio\":\"09:00\",\"fin\":\"18:00\"}],\"sab\":[{\"inicio\":\"09:00\",\"fin\":\"14:00\"}]}", // Horario estándar
                Comisiones = "{\"porcentaje_servicios\":10}" // Comisión básica del 10%
            };

            return Page();
        }

        [BindProperty]
        public Empleado Empleado { get; set; } = default!;

        [BindProperty]
        public List<string> EspecialidadesSeleccionadas { get; set; } = new();

        public async Task<IActionResult> OnPostAsync()
        {   
            await LoadSucursalesAsync();
            await LoadPuestosAsync();

            // VALIDAR ACCESO A LA SUCURSAL SELECCIONADA
            var userId = HttpContext.Session.GetString("UserId");
            if (long.TryParse(userId, out var usuarioId))
            {
                var tieneAcceso = await _sucursalFilter.UsuarioTieneAccesoSucursalAsync(usuarioId, Empleado.SucursalId);
                if (!tieneAcceso)
                {
                    ModelState.AddModelError("Empleado.SucursalId", "No tienes permisos para crear empleados en esta sucursal.");
                }
            }

            // Remover validaciones de campos automáticos
            ModelState.Remove("Empleado.Id");
            ModelState.Remove("Empleado.CreatedAt");
            ModelState.Remove("Empleado.UpdatedAt");
            ModelState.Remove("Empleado.Sucursal");
            ModelState.Remove("Empleado.PuestoNavegacion");

            // Generar código de empleado automáticamente si no se proporciona
            if (string.IsNullOrEmpty(Empleado.CodigoEmpleado))
            {
                var maxCodigo = await _context.Empleados
                    .Where(e => e.CodigoEmpleado.StartsWith("EMP"))
                    .Select(e => e.CodigoEmpleado)
                    .OrderByDescending(c => c)
                    .FirstOrDefaultAsync();

                var siguienteNumero = 1;
                if (!string.IsNullOrEmpty(maxCodigo) && maxCodigo.Length >= 6)
                {
                    var numeroStr = maxCodigo.Substring(3);
                    if (int.TryParse(numeroStr, out var numero))
                    {
                        siguienteNumero = numero + 1;
                    }
                }
                Empleado.CodigoEmpleado = $"EMP{siguienteNumero:D3}";
            }

            // Configurar valores predefinidos
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
                Empleado.Especialidades = System.Text.Json.JsonSerializer.Serialize(EspecialidadesSeleccionadas);
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                Empleado.CreatedAt = DateTime.UtcNow;
                _context.Empleados.Add(Empleado);
                await _context.SaveChangesAsync();

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error al crear el empleado: {ex.Message}");
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