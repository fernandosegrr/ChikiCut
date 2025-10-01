using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Services;
using ChikiCut.web.Attributes;

namespace ChikiCut.web.Pages.Productos
{
    [RequirePermission("productos", "create")]
    public class AsignarModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ISucursalFilterService _sucursalFilter;

        public AsignarModel(AppDbContext context, ISucursalFilterService sucursalFilter)
        {
            _context = context;
            _sucursalFilter = sucursalFilter;
        }

        [BindProperty]
        public Producto Producto { get; set; } = default!;

        [BindProperty]
        public List<SucursalAsignacionForm> Asignaciones { get; set; } = new();

        public List<SucursalAsignacion> SucursalesDisponibles { get; set; } = new();
        public bool TieneAccesoGlobal { get; set; }
        public List<string> SucursalesUsuario { get; set; } = new();

        public class SucursalAsignacion
        {
            public Sucursal Sucursal { get; set; } = default!;
            public ProductoSucursal? AsignacionActual { get; set; }
            public bool EstaAsignado { get; set; }
        }

        public class SucursalAsignacionForm
        {
            public long SucursalId { get; set; }
            public string SucursalNombre { get; set; } = string.Empty;
            public bool Asignado { get; set; }
            public decimal PrecioVenta { get; set; }
            public decimal? PrecioMayoreo { get; set; }
            public int CantidadMayoreo { get; set; } = 10;
            public decimal? CostoLocal { get; set; }
            public int StockInicial { get; set; }
            public int? StockMinimoLocal { get; set; }
            public int? StockMaximoLocal { get; set; }
            public int? PuntoReordenLocal { get; set; }
            public bool Disponible { get; set; } = true;
            public bool SePuedeVender { get; set; } = true;
            public bool SePuedeReservar { get; set; } = true;
            public decimal DescuentoMaximoLocal { get; set; } = 0.00m;
            public string? UbicacionFisica { get; set; }
            public string? SeccionDisplay { get; set; }
            public bool EsDestacadoLocal { get; set; }
            public string? ObservacionesLocales { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null)
            {
                return NotFound();
            }

            Producto = producto;

            // Obtener ID del usuario actual
            var userId = HttpContext.Session.GetString("UserId");
            if (!long.TryParse(userId, out var usuarioId))
            {
                return Challenge();
            }

            // Verificar acceso global
            TieneAccesoGlobal = await _sucursalFilter.TieneAccesoGlobalAsync(usuarioId);

            await CargarSucursalesAsync(usuarioId);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long? id)
        {
            if (id == null || id <= 0)
            {
                return NotFound();
            }

            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null)
            {
                return NotFound();
            }

            Producto = producto;

            // Obtener ID del usuario actual
            var userId = HttpContext.Session.GetString("UserId");
            if (!long.TryParse(userId, out var usuarioId))
            {
                return Challenge();
            }

            TieneAccesoGlobal = await _sucursalFilter.TieneAccesoGlobalAsync(usuarioId);

            if (!ModelState.IsValid)
            {
                await CargarSucursalesAsync(usuarioId);
                return Page();
            }

            try
            {
                // Obtener asignaciones actuales
                var asignacionesActuales = await _context.ProductoSucursales
                    .Where(ps => ps.ProductoId == id)
                    .ToDictionaryAsync(ps => ps.SucursalId, ps => ps);

                foreach (var asignacion in Asignaciones)
                {
                    var asignacionExistente = asignacionesActuales.GetValueOrDefault(asignacion.SucursalId);

                    if (asignacion.Asignado)
                    {
                        if (asignacionExistente == null)
                        {
                            // Crear nueva asignación
                            var nuevaAsignacion = new ProductoSucursal
                            {
                                ProductoId = id.Value,
                                SucursalId = asignacion.SucursalId,
                                PrecioVenta = asignacion.PrecioVenta,
                                PrecioMayoreo = asignacion.PrecioVenta * 0.9m,
                                CantidadMayoreo = 10,
                                StockActual = asignacion.StockInicial,
                                StockMinimoLocal = Producto.RequiereInventario ? Producto.StockMinimo : null,
                                StockMaximoLocal = Producto.RequiereInventario ? Producto.StockMaximo : null,
                                PuntoReordenLocal = Producto.RequiereInventario ? Producto.PuntoReorden : null,
                                Disponible = asignacion.Disponible,
                                SePuedeVender = true,
                                SePuedeReservar = true,
                                DescuentoMaximoLocal = Producto.DescuentoMaximo,
                                EsDestacadoLocal = asignacion.EsDestacadoLocal,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow,
                                CreatedBy = usuarioId
                            };
                            _context.ProductoSucursales.Add(nuevaAsignacion);
                        }
                        else
                        {
                            // Actualizar asignación existente
                            asignacionExistente.PrecioVenta = asignacion.PrecioVenta;
                            asignacionExistente.PrecioMayoreo = asignacion.PrecioVenta * 0.9m;
                            
                            // Solo actualizar stock si es diferente (para no sobrescribir movimientos)
                            if (asignacionExistente.StockActual == 0 || !asignacionExistente.IsActive)
                            {
                                asignacionExistente.StockActual = asignacion.StockInicial;
                            }
                            
                            asignacionExistente.Disponible = asignacion.Disponible;
                            asignacionExistente.EsDestacadoLocal = asignacion.EsDestacadoLocal;
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
                TempData["SuccessMessage"] = $"Producto '{Producto.Nombre}' actualizado. Asignado a {sucursalesAsignadas} sucursal(es).";

                return RedirectToPage("./Details", new { id = id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al guardar las asignaciones: {ex.Message}");
                await CargarSucursalesAsync(usuarioId);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostCalcularPrecioAsync(decimal precioBase, decimal margen)
        {
            if (precioBase <= 0 || margen < 0)
            {
                return new JsonResult(new { success = false, message = "Valores inválidos" });
            }

            var precioCalculado = precioBase * (1 + margen / 100);
            var precioMayoreo = precioCalculado * 0.9m; // 10% menos para mayoreo

            return new JsonResult(new 
            { 
                success = true, 
                precioVenta = Math.Round(precioCalculado, 2),
                precioMayoreo = Math.Round(precioMayoreo, 2)
            });
        }

        public async Task<IActionResult> OnPostAplicarConfiguracionAsync(long productoId, string configuracion)
        {
            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null)
            {
                return new JsonResult(new { success = false, message = "Producto no encontrado" });
            }

            decimal precioBase = producto.PrecioBase;
            decimal margen = producto.MargenGananciaSugerido;

            var configuraciones = new Dictionary<string, object>();

            switch (configuracion.ToLower())
            {
                case "basico":
                    configuraciones["precioVenta"] = precioBase;
                    configuraciones["precioMayoreo"] = Math.Round(precioBase * 0.9m, 2);
                    configuraciones["stockInicial"] = 10;
                    configuraciones["descuentoMaximo"] = producto.DescuentoMaximo;
                    break;

                case "premium":
                    var precioPremium = Math.Round(precioBase * 1.15m, 2);
                    configuraciones["precioVenta"] = precioPremium;
                    configuraciones["precioMayoreo"] = Math.Round(precioPremium * 0.9m, 2);
                    configuraciones["stockInicial"] = 20;
                    configuraciones["descuentoMaximo"] = Math.Max(0, producto.DescuentoMaximo - 5);
                    configuraciones["esDestacado"] = true;
                    break;

                case "economico":
                    var precioEconomico = Math.Round(precioBase * 0.95m, 2);
                    configuraciones["precioVenta"] = precioEconomico;
                    configuraciones["precioMayoreo"] = Math.Round(precioEconomico * 0.9m, 2);
                    configuraciones["stockInicial"] = 15;
                    configuraciones["descuentoMaximo"] = Math.Min(100, producto.DescuentoMaximo + 5);
                    break;

                default:
                    return new JsonResult(new { success = false, message = "Configuración no válida" });
            }

            return new JsonResult(new { success = true, configuracion = configuraciones });
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
            var asignacionesActuales = await _context.ProductoSucursales
                .Where(ps => ps.ProductoId == Producto.Id && ps.IsActive)
                .ToDictionaryAsync(ps => ps.SucursalId, ps => ps);

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
                PrecioVenta = sa.AsignacionActual?.PrecioVenta ?? Producto.PrecioBase,
                PrecioMayoreo = sa.AsignacionActual?.PrecioMayoreo ?? (Producto.PrecioBase * 0.9m),
                CantidadMayoreo = sa.AsignacionActual?.CantidadMayoreo ?? 10,
                CostoLocal = sa.AsignacionActual?.CostoLocal ?? Producto.CostoPromedio,
                StockInicial = sa.AsignacionActual?.StockActual ?? (Producto.RequiereInventario ? 10 : 0),
                StockMinimoLocal = sa.AsignacionActual?.StockMinimoLocal,
                StockMaximoLocal = sa.AsignacionActual?.StockMaximoLocal,
                PuntoReordenLocal = sa.AsignacionActual?.PuntoReordenLocal,
                Disponible = sa.AsignacionActual?.Disponible ?? true,
                SePuedeVender = sa.AsignacionActual?.SePuedeVender ?? true,
                SePuedeReservar = sa.AsignacionActual?.SePuedeReservar ?? true,
                DescuentoMaximoLocal = sa.AsignacionActual?.DescuentoMaximoLocal ?? Producto.DescuentoMaximo,
                UbicacionFisica = sa.AsignacionActual?.UbicacionFisica,
                SeccionDisplay = sa.AsignacionActual?.SeccionDisplay,
                EsDestacadoLocal = sa.AsignacionActual?.EsDestacadoLocal ?? false,
                ObservacionesLocales = sa.AsignacionActual?.ObservacionesLocales
            }).ToList();
        }
    }
}