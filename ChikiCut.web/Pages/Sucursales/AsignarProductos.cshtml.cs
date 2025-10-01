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
    public class AsignarProductosModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ISucursalFilterService _sucursalFilter;

        public AsignarProductosModel(AppDbContext context, ISucursalFilterService sucursalFilter)
        {
            _context = context;
            _sucursalFilter = sucursalFilter;
        }

        [BindProperty]
        public long SucursalId { get; set; }

        [BindProperty]
        public List<ProductoAsignacionForm> ProductosAsignacion { get; set; } = new();

        public Sucursal Sucursal { get; set; } = default!;
        public List<ProductoInfo> ProductosDisponibles { get; set; } = new();
        public List<string> CategoriasDisponibles { get; set; } = new();
        public List<string> MarcasDisponibles { get; set; } = new();
        
        // Filtros
        public string? CategoriaFilter { get; set; }
        public string? MarcaFilter { get; set; }
        public string? SearchString { get; set; }
        public bool? SoloNoAsignados { get; set; }

        // Estadísticas
        public int TotalProductos { get; set; }
        public int ProductosAsignados { get; set; }
        public int ProductosSeleccionados { get; set; }

        public class ProductoInfo
        {
            public Producto Producto { get; set; } = default!;
            public ProductoSucursal? AsignacionActual { get; set; }
            public bool EstaAsignado { get; set; }
            public bool Seleccionado { get; set; }
        }

        public class ProductoAsignacionForm
        {
            public long ProductoId { get; set; }
            public string ProductoNombre { get; set; } = string.Empty;
            public bool Asignar { get; set; }
            public decimal PrecioVenta { get; set; }
            public int StockInicial { get; set; }
            public bool Disponible { get; set; } = true;
            public bool EsDestacado { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(long? id, string? categoria, string? marca, string? search, bool? soloNoAsignados)
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
            MarcaFilter = marca;
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

            await CargarProductosYFiltrosAsync();
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

                var productosParaAsignar = ProductosAsignacion.Where(p => p.Asignar).ToList();
                var productosParaDesasignar = ProductosAsignacion.Where(p => !p.Asignar).ToList();

                var asignacionesExistentes = await _context.ProductoSucursales
                    .Where(ps => ps.SucursalId == SucursalId)
                    .ToDictionaryAsync(ps => ps.ProductoId, ps => ps);

                var contadorAsignados = 0;
                var contadorDesasignados = 0;

                // Procesar asignaciones
                foreach (var productoForm in productosParaAsignar)
                {
                    var producto = await _context.Productos.FindAsync(productoForm.ProductoId);
                    if (producto == null) continue;

                    var asignacionExistente = asignacionesExistentes.GetValueOrDefault(productoForm.ProductoId);

                    if (asignacionExistente == null)
                    {
                        // Crear nueva asignación
                        var nuevaAsignacion = new ProductoSucursal
                        {
                            ProductoId = productoForm.ProductoId,
                            SucursalId = SucursalId,
                            PrecioVenta = productoForm.PrecioVenta,
                            PrecioMayoreo = productoForm.PrecioVenta * 0.9m,
                            CantidadMayoreo = 10,
                            StockActual = producto.RequiereInventario ? productoForm.StockInicial : 0,
                            StockMinimoLocal = producto.RequiereInventario ? producto.StockMinimo : null,
                            StockMaximoLocal = producto.RequiereInventario ? producto.StockMaximo : null,
                            PuntoReordenLocal = producto.RequiereInventario ? producto.PuntoReorden : null,
                            Disponible = productoForm.Disponible,
                            SePuedeVender = true,
                            SePuedeReservar = true,
                            DescuentoMaximoLocal = producto.DescuentoMaximo,
                            EsDestacadoLocal = productoForm.EsDestacado,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.ProductoSucursales.Add(nuevaAsignacion);
                        contadorAsignados++;
                    }
                    else if (!asignacionExistente.IsActive)
                    {
                        // Reactivar asignación existente
                        asignacionExistente.IsActive = true;
                        asignacionExistente.PrecioVenta = productoForm.PrecioVenta;
                        asignacionExistente.StockActual = producto.RequiereInventario ? productoForm.StockInicial : 0;
                        asignacionExistente.Disponible = productoForm.Disponible;
                        asignacionExistente.EsDestacadoLocal = productoForm.EsDestacado;
                        asignacionExistente.UpdatedAt = DateTime.UtcNow;
                        contadorAsignados++;
                    }
                }

                // Procesar desasignaciones
                foreach (var productoForm in productosParaDesasignar)
                {
                    var asignacionExistente = asignacionesExistentes.GetValueOrDefault(productoForm.ProductoId);
                    if (asignacionExistente != null && asignacionExistente.IsActive)
                    {
                        asignacionExistente.IsActive = false;
                        asignacionExistente.UpdatedAt = DateTime.UtcNow;
                        contadorDesasignados++;
                    }
                }

                await _context.SaveChangesAsync();

                var mensaje = $"Asignación completada: {contadorAsignados} productos asignados";
                if (contadorDesasignados > 0)
                {
                    mensaje += $", {contadorDesasignados} productos desasignados";
                }

                TempData["SuccessMessage"] = mensaje + $" para la sucursal {Sucursal.Name}.";

                return RedirectToPage(new { id = SucursalId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al procesar asignaciones: {ex.Message}");
                await CargarProductosYFiltrosAsync();
                CalcularEstadisticas();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAplicarConfiguracionMasivaAsync()
        {
            try
            {
                var configuracion = Request.Form["Configuracion"].FirstOrDefault();
                var productosSeleccionados = Request.Form["ProductosSeleccionados"].ToList();
                var precio = decimal.Parse(Request.Form["PrecioMasivo"].FirstOrDefault() ?? "0");
                var stock = int.Parse(Request.Form["StockMasivo"].FirstOrDefault() ?? "0");

                if (productosSeleccionados.Count == 0)
                {
                    return new JsonResult(new { success = false, message = "No se seleccionaron productos" });
                }

                var resultados = new List<object>();

                foreach (var productoIdStr in productosSeleccionados)
                {
                    if (long.TryParse(productoIdStr, out var productoId))
                    {
                        var producto = await _context.Productos.FindAsync(productoId);
                        if (producto != null)
                        {
                            var precioCalculado = configuracion switch
                            {
                                "base" => producto.PrecioBase,
                                "personalizado" => precio,
                                "margen" => producto.CostoPromedio.HasValue 
                                    ? producto.CostoPromedio.Value * (1 + producto.MargenGananciaSugerido / 100)
                                    : producto.PrecioBase,
                                _ => producto.PrecioBase
                            };

                            var stockCalculado = configuracion switch
                            {
                                "base" => producto.RequiereInventario ? 10 : 0,
                                "personalizado" => stock,
                                "minimo" => producto.RequiereInventario ? producto.StockMinimo : 0,
                                _ => producto.RequiereInventario ? 10 : 0
                            };

                            resultados.Add(new
                            {
                                productoId = productoId,
                                nombre = producto.Nombre,
                                precio = Math.Round(precioCalculado, 2),
                                stock = stockCalculado
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

        private async Task CargarProductosYFiltrosAsync()
        {
            // Cargar filtros disponibles primero
            CategoriasDisponibles = await _context.Productos
                .Where(p => p.IsActive)
                .Select(p => p.Categoria)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            MarcasDisponibles = await _context.Productos
                .Where(p => p.IsActive)
                .Select(p => p.Marca)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();

            // Construir query base
            var productos = await _context.Productos
                .Where(p => p.IsActive &&
                    (string.IsNullOrEmpty(CategoriaFilter) || p.Categoria == CategoriaFilter) &&
                    (string.IsNullOrEmpty(MarcaFilter) || p.Marca == MarcaFilter) &&
                    (string.IsNullOrEmpty(SearchString) || 
                        p.Nombre.ToLower().Contains(SearchString.ToLower()) ||
                        p.Codigo.ToLower().Contains(SearchString.ToLower()) ||
                        p.Marca.ToLower().Contains(SearchString.ToLower())))
                .Include(p => p.SucursalesAsignadas.Where(ps => ps.SucursalId == SucursalId && ps.IsActive))
                .OrderBy(p => p.Categoria)
                .ThenBy(p => p.Nombre)
                .ToListAsync();

            // Preparar productos con información de asignación
            ProductosDisponibles = productos.Select(p => new ProductoInfo
            {
                Producto = p,
                AsignacionActual = p.SucursalesAsignadas.FirstOrDefault(),
                EstaAsignado = p.SucursalesAsignadas.Any(),
                Seleccionado = false
            }).ToList();

            // Aplicar filtro de solo no asignados si está activo
            if (SoloNoAsignados == true)
            {
                ProductosDisponibles = ProductosDisponibles.Where(p => !p.EstaAsignado).ToList();
            }
            else if (SoloNoAsignados == false)
            {
                ProductosDisponibles = ProductosDisponibles.Where(p => p.EstaAsignado).ToList();
            }
        }

        private void CalcularEstadisticas()
        {
            TotalProductos = ProductosDisponibles.Count;
            ProductosAsignados = ProductosDisponibles.Count(p => p.EstaAsignado);
            ProductosSeleccionados = ProductosDisponibles.Count(p => p.Seleccionado);
        }
    }
}