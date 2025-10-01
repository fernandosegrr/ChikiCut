using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Services;
using ChikiCut.web.Attributes;

namespace ChikiCut.web.Pages.Servicios
{
    [RequirePermission("servicios", "assign")]
    public class AsignarModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ISucursalFilterService _sucursalFilter;

        public AsignarModel(AppDbContext context, ISucursalFilterService sucursalFilter)
        {
            _context = context;
            _sucursalFilter = sucursalFilter;
        }

        public Servicio Servicio { get; set; } = default!;
        public List<SucursalAsignacion> SucursalesDisponibles { get; set; } = new();
        public bool TieneAccesoGlobal { get; set; }
        public List<string> SucursalesUsuario { get; set; } = new();

        [BindProperty]
        public List<SucursalAsignacionForm> Asignaciones { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Cargar servicio
            var servicio = await _context.Servicios
                .FirstOrDefaultAsync(s => s.Id == id);

            if (servicio == null)
            {
                return NotFound();
            }

            Servicio = servicio;

            // Obtener información del usuario actual
            var userId = HttpContext.Session.GetString("UserId");
            if (!long.TryParse(userId, out var usuarioId))
            {
                return RedirectToPage("/Account/Login");
            }

            // Verificar permisos y cargar sucursales
            TieneAccesoGlobal = await _sucursalFilter.TieneAccesoGlobalAsync(usuarioId);
            await CargarSucursalesAsync(usuarioId);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Recargar servicio
            var servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Id == id);
            if (servicio == null)
            {
                return NotFound();
            }

            Servicio = servicio;

            // Validar permisos del usuario
            var userId = HttpContext.Session.GetString("UserId");
            if (!long.TryParse(userId, out var usuarioId))
            {
                return RedirectToPage("/Account/Login");
            }

            // Verificar acceso a sucursales seleccionadas
            foreach (var asignacion in Asignaciones.Where(a => a.Asignado))
            {
                var tieneAcceso = await _sucursalFilter.UsuarioTieneAccesoSucursalAsync(usuarioId, asignacion.SucursalId);
                if (!tieneAcceso)
                {
                    ModelState.AddModelError("", $"No tienes permisos para asignar servicios a una o más sucursales seleccionadas.");
                    await CargarSucursalesAsync(usuarioId);
                    return Page();
                }

                if (asignacion.PrecioLocal <= 0)
                {
                    ModelState.AddModelError("", $"El precio debe ser mayor a cero para todas las sucursales seleccionadas.");
                    await CargarSucursalesAsync(usuarioId);
                    return Page();
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarSucursalesAsync(usuarioId);
                return Page();
            }

            try
            {
                // Obtener asignaciones actuales
                var asignacionesActuales = await _context.ServicioSucursales
                    .Where(ss => ss.ServicioId == id)
                    .ToListAsync();

                // Procesar cada asignación
                foreach (var asignacion in Asignaciones)
                {
                    var asignacionExistente = asignacionesActuales
                        .FirstOrDefault(ss => ss.SucursalId == asignacion.SucursalId);

                    if (asignacion.Asignado)
                    {
                        if (asignacionExistente == null)
                        {
                            // Crear nueva asignación
                            var nuevaAsignacion = new ServicioSucursal
                            {
                                ServicioId = id.Value,
                                SucursalId = asignacion.SucursalId,
                                PrecioLocal = asignacion.PrecioLocal,
                                DuracionLocal = asignacion.DuracionLocal > 0 ? asignacion.DuracionLocal : null,
                                Disponible = true,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow,
                                CreatedBy = usuarioId,
                                DescuentoMaximo = asignacion.DescuentoMaximo
                            };
                            _context.ServicioSucursales.Add(nuevaAsignacion);
                        }
                        else
                        {
                            // Actualizar asignación existente
                            asignacionExistente.PrecioLocal = asignacion.PrecioLocal;
                            asignacionExistente.DuracionLocal = asignacion.DuracionLocal > 0 ? asignacion.DuracionLocal : null;
                            asignacionExistente.DescuentoMaximo = asignacion.DescuentoMaximo;
                            asignacionExistente.Disponible = true;
                            asignacionExistente.IsActive = true;
                            asignacionExistente.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                    else if (asignacionExistente != null)
                    {
                        // Desactivar asignación
                        asignacionExistente.IsActive = false;
                        asignacionExistente.Disponible = false;
                        asignacionExistente.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();

                var sucursalesAsignadas = Asignaciones.Count(a => a.Asignado);
                TempData["SuccessMessage"] = $"Servicio '{Servicio.Nombre}' actualizado. Asignado a {sucursalesAsignadas} sucursal(es).";

                return RedirectToPage("./Details", new { id = id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al guardar las asignaciones: {ex.Message}");
                await CargarSucursalesAsync(usuarioId);
                return Page();
            }
        }

        private async Task CargarSucursalesAsync(long usuarioId)
        {
            List<Sucursal> sucursales;

            if (TieneAccesoGlobal)
            {
                // Mostrar todas las sucursales
                sucursales = await _context.Sucursals
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Name)
                    .ToListAsync();
            }
            else
            {
                // Solo sucursales con acceso
                var sucursalesIds = await _sucursalFilter.GetSucursalesUsuarioAsync(usuarioId);
                sucursales = await _context.Sucursals
                    .Where(s => s.IsActive && sucursalesIds.Contains(s.Id))
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                SucursalesUsuario = sucursales.Select(s => s.Name).ToList();
            }

            // Obtener asignaciones actuales
            var asignacionesActuales = await _context.ServicioSucursales
                .Where(ss => ss.ServicioId == Servicio.Id && ss.IsActive)
                .ToDictionaryAsync(ss => ss.SucursalId, ss => ss);

            // Preparar datos para la vista
            SucursalesDisponibles = sucursales.Select(s => new SucursalAsignacion
            {
                Sucursal = s,
                AsignacionActual = asignacionesActuales.GetValueOrDefault(s.Id),
                EstaAsignado = asignacionesActuales.ContainsKey(s.Id)
            }).ToList();

            // Preparar datos para el formulario
            Asignaciones = SucursalesDisponibles.Select(sa => new SucursalAsignacionForm
            {
                SucursalId = sa.Sucursal.Id,
                SucursalNombre = sa.Sucursal.Name,
                Asignado = sa.EstaAsignado,
                PrecioLocal = sa.AsignacionActual?.PrecioLocal ?? Servicio.PrecioBase,
                DuracionLocal = sa.AsignacionActual?.DuracionLocal ?? 0,
                DescuentoMaximo = sa.AsignacionActual?.DescuentoMaximo ?? 0
            }).ToList();
        }

        public class SucursalAsignacion
        {
            public Sucursal Sucursal { get; set; } = default!;
            public ServicioSucursal? AsignacionActual { get; set; }
            public bool EstaAsignado { get; set; }
        }

        public class SucursalAsignacionForm
        {
            public long SucursalId { get; set; }
            public string SucursalNombre { get; set; } = string.Empty;
            public bool Asignado { get; set; }
            public decimal PrecioLocal { get; set; }
            public int DuracionLocal { get; set; }
            public decimal DescuentoMaximo { get; set; }
        }
    }
}