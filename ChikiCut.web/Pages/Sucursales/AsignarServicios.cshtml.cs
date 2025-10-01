using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Attributes;
using ChikiCut.web.Services;

namespace ChikiCut.web.Pages.Sucursales
{
    [RequirePermission("sucursales", "update")]
    public class AsignarServiciosModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ISucursalFilterService _sucursalFilter;

        public AsignarServiciosModel(AppDbContext context, ISucursalFilterService sucursalFilter)
        {
            _context = context;
            _sucursalFilter = sucursalFilter;
        }

        [BindProperty]
        public long SucursalId { get; set; }

        [BindProperty]
        public List<ServicioAsignacionForm> ServiciosAsignacion { get; set; } = new();

        public Sucursal Sucursal { get; set; } = default!;
        public List<ServicioInfo> ServiciosDisponibles { get; set; } = new();
        public List<string> CategoriasDisponibles { get; set; } = new();
        
        // Filtros
        public string? CategoriaFilter { get; set; }
        public string? SearchString { get; set; }
        public bool? SoloNoAsignados { get; set; }

        // Estadísticas
        public int TotalServicios { get; set; }
        public int ServiciosAsignados { get; set; }
        public int ServiciosSeleccionados { get; set; }

        public class ServicioInfo
        {
            public Servicio Servicio { get; set; } = default!;
            public ServicioSucursal? AsignacionActual { get; set; }
            public bool EstaAsignado { get; set; }
            public bool Seleccionado { get; set; }
        }

        public class ServicioAsignacionForm
        {
            public long ServicioId { get; set; }
            public string ServicioNombre { get; set; } = string.Empty;
            public bool Asignar { get; set; }
            public decimal PrecioLocal { get; set; }
            public int DuracionLocal { get; set; }
            public decimal DescuentoMaximo { get; set; }
            public bool Disponible { get; set; } = true;
            public bool EsDestacado { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(long? id, string? categoria, string? search, bool? soloNoAsignados)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sucursal = await _context.Sucursals.FirstOrDefaultAsync(s => s.Id == id && s.IsActive);
            if (sucursal == null)
            {
                return NotFound();
            }

            Sucursal = sucursal;
            SucursalId = id.Value;

            // Aplicar filtros
            CategoriaFilter = categoria;
            SearchString = search;
            SoloNoAsignados = soloNoAsignados;

            // Verificar acceso del usuario a esta sucursal
            var userId = HttpContext.Session.GetString("UserId");
            if (long.TryParse(userId, out var usuarioId))
            {
                var tieneAccesoGlobal = await _sucursalFilter.TieneAccesoGlobalAsync(usuarioId);
                if (!tieneAccesoGlobal)
                {
                    var tieneAcceso = await _sucursalFilter.UsuarioTieneAccesoSucursalAsync(usuarioId, id.Value);
                    if (!tieneAcceso)
                    {
                        return Forbid();
                    }
                }
            }

            await CargarServiciosYFiltrosAsync();
            CalcularEstadisticas();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var sucursal = await _context.Sucursals.FindAsync(SucursalId);
                if (sucursal == null)
                {
                    return NotFound();
                }

                Sucursal = sucursal;

                var serviciosParaAsignar = ServiciosAsignacion.Where(s => s.Asignar).ToList();
                var serviciosParaDesasignar = ServiciosAsignacion.Where(s => !s.Asignar).ToList();

                var asignacionesExistentes = await _context.ServicioSucursales
                    .Where(ss => ss.SucursalId == SucursalId)
                    .ToDictionaryAsync(ss => ss.ServicioId, ss => ss);

                var contadorAsignados = 0;
                var contadorDesasignados = 0;

                // Procesar asignaciones
                foreach (var servicioForm in serviciosParaAsignar)
                {
                    var servicio = await _context.Servicios.FindAsync(servicioForm.ServicioId);
                    if (servicio == null) continue;

                    var asignacionExistente = asignacionesExistentes.GetValueOrDefault(servicioForm.ServicioId);

                    if (asignacionExistente == null)
                    {
                        // Crear nueva asignación
                        var nuevaAsignacion = new ServicioSucursal
                        {
                            ServicioId = servicioForm.ServicioId,
                            SucursalId = SucursalId,
                            PrecioLocal = servicioForm.PrecioLocal,
                            DuracionLocal = servicioForm.DuracionLocal > 0 ? servicioForm.DuracionLocal : servicio.DuracionEstimada,
                            DescuentoMaximo = servicioForm.DescuentoMaximo > 0 ? servicioForm.DescuentoMaximo : 0,
                            Disponible = servicioForm.Disponible,
                            RequiereCitaLocal = servicio.RequiereCita,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.ServicioSucursales.Add(nuevaAsignacion);
                        contadorAsignados++;
                    }
                    else if (!asignacionExistente.IsActive)
                    {
                        // Reactivar asignación existente
                        asignacionExistente.IsActive = true;
                        asignacionExistente.PrecioLocal = servicioForm.PrecioLocal;
                        asignacionExistente.DuracionLocal = servicioForm.DuracionLocal > 0 ? servicioForm.DuracionLocal : servicio.DuracionEstimada;
                        asignacionExistente.Disponible = servicioForm.Disponible;
                        asignacionExistente.UpdatedAt = DateTime.UtcNow;
                        contadorAsignados++;
                    }
                }

                // Procesar desasignaciones
                foreach (var servicioForm in serviciosParaDesasignar)
                {
                    var asignacionExistente = asignacionesExistentes.GetValueOrDefault(servicioForm.ServicioId);
                    if (asignacionExistente != null && asignacionExistente.IsActive)
                    {
                        asignacionExistente.IsActive = false;
                        asignacionExistente.UpdatedAt = DateTime.UtcNow;
                        contadorDesasignados++;
                    }
                }

                await _context.SaveChangesAsync();

                var mensaje = $"Asignación completada: {contadorAsignados} servicios asignados";
                if (contadorDesasignados > 0)
                {
                    mensaje += $", {contadorDesasignados} servicios desasignados";
                }

                TempData["SuccessMessage"] = mensaje + $" para la sucursal {Sucursal.Name}.";

                return RedirectToPage(new { id = SucursalId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al procesar asignaciones: {ex.Message}");
                await CargarServiciosYFiltrosAsync();
                CalcularEstadisticas();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAplicarConfiguracionMasivaAsync()
        {
            try
            {
                var configuracion = Request.Form["Configuracion"].FirstOrDefault();
                var serviciosSeleccionados = Request.Form["ServiciosSeleccionados"].ToList();
                var precio = decimal.Parse(Request.Form["PrecioMasivo"].FirstOrDefault() ?? "0");
                var duracion = int.Parse(Request.Form["DuracionMasiva"].FirstOrDefault() ?? "0");

                if (serviciosSeleccionados.Count == 0)
                {
                    return new JsonResult(new { success = false, message = "No se seleccionaron servicios" });
                }

                var resultados = new List<object>();

                foreach (var servicioIdStr in serviciosSeleccionados)
                {
                    if (long.TryParse(servicioIdStr, out var servicioId))
                    {
                        var servicio = await _context.Servicios.FindAsync(servicioId);
                        if (servicio != null)
                        {
                            var precioCalculado = configuracion switch
                            {
                                "base" => servicio.PrecioBase,
                                "personalizado" => precio,
                                "margen" => servicio.PrecioBase * 1.2m, // Margen fijo del 20%
                                _ => servicio.PrecioBase
                            };

                            var duracionCalculada = configuracion switch
                            {
                                "base" => servicio.DuracionEstimada,
                                "personalizado" => duracion,
                                _ => servicio.DuracionEstimada
                            };

                            resultados.Add(new
                            {
                                servicioId = servicioId,
                                nombre = servicio.Nombre,
                                precio = Math.Round(precioCalculado, 2),
                                duracion = duracionCalculada
                            });
                        }
                    }
                }

                return new JsonResult(new { success = true, resultados = resultados });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        private async Task CargarServiciosYFiltrosAsync()
        {
            // Cargar filtros disponibles primero
            CategoriasDisponibles = await _context.Servicios
                .Where(s => s.IsActive)
                .Select(s => s.Categoria)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            // Construir query base
            var servicios = await _context.Servicios
                .Where(s => s.IsActive &&
                    (string.IsNullOrEmpty(CategoriaFilter) || s.Categoria == CategoriaFilter) &&
                    (string.IsNullOrEmpty(SearchString) || 
                        s.Nombre.ToLower().Contains(SearchString.ToLower()) ||
                        s.Codigo.ToLower().Contains(SearchString.ToLower())))
                .Include(s => s.SucursalesAsignadas.Where(ss => ss.SucursalId == SucursalId && ss.IsActive))
                .OrderBy(s => s.Categoria)
                .ThenBy(s => s.Nombre)
                .ToListAsync();

            // Preparar servicios con información de asignación
            ServiciosDisponibles = servicios.Select(s => new ServicioInfo
            {
                Servicio = s,
                AsignacionActual = s.SucursalesAsignadas.FirstOrDefault(),
                EstaAsignado = s.SucursalesAsignadas.Any(),
                Seleccionado = false
            }).ToList();

            // Aplicar filtro de solo no asignados si está activo
            if (SoloNoAsignados == true)
            {
                ServiciosDisponibles = ServiciosDisponibles.Where(s => !s.EstaAsignado).ToList();
            }
            else if (SoloNoAsignados == false)
            {
                ServiciosDisponibles = ServiciosDisponibles.Where(s => s.EstaAsignado).ToList();
            }
        }

        private void CalcularEstadisticas()
        {
            TotalServicios = ServiciosDisponibles.Count;
            ServiciosAsignados = ServiciosDisponibles.Count(s => s.EstaAsignado);
            ServiciosSeleccionados = ServiciosDisponibles.Count(s => s.Seleccionado);
        }
    }
}