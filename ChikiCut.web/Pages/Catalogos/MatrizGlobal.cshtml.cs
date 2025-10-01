using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Attributes;
using ChikiCut.web.Services;

namespace ChikiCut.web.Pages.Catalogos
{
    [RequirePermission("productos", "read")]
    public class MatrizGlobalModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ISucursalFilterService _sucursalFilter;

        public MatrizGlobalModel(AppDbContext context, ISucursalFilterService sucursalFilter)
        {
            _context = context;
            _sucursalFilter = sucursalFilter;
        }

        // Propiedades para la vista
        public List<Sucursal> SucursalesDisponibles { get; set; } = new();
        public List<ProductoMatriz> ProductosMatriz { get; set; } = new();
        public List<ServicioMatriz> ServiciosMatriz { get; set; } = new();
        
        // Filtros
        public string TipoVista { get; set; } = "productos"; // productos, servicios, ambos
        public string? CategoriaFilter { get; set; }
        public string? MarcaFilter { get; set; }
        public string? SearchString { get; set; }
        public bool SoloConAsignaciones { get; set; }
        public bool SoloSinAsignaciones { get; set; }

        // Estadísticas
        public EstadisticasMatriz Estadisticas { get; set; } = new();

        // Clases para el modelo
        public class ProductoMatriz
        {
            public Producto Producto { get; set; } = default!;
            public Dictionary<long, ProductoSucursal?> AsignacionesPorSucursal { get; set; } = new();
            public int TotalSucursalesAsignadas { get; set; }
            public decimal PrecioPromedio { get; set; }
            public string RangoPrecio { get; set; } = "";
        }

        public class ServicioMatriz
        {
            public Servicio Servicio { get; set; } = default!;
            public Dictionary<long, ServicioSucursal?> AsignacionesPorSucursal { get; set; } = new();
            public int TotalSucursalesAsignadas { get; set; }
            public decimal PrecioPromedio { get; set; }
            public string RangoPrecio { get; set; } = "";
        }

        public class EstadisticasMatriz
        {
            public int TotalProductos { get; set; }
            public int ProductosConAsignaciones { get; set; }
            public int ProductosSinAsignaciones { get; set; }
            public int TotalServicios { get; set; }
            public int ServiciosConAsignaciones { get; set; }
            public int ServiciosSinAsignaciones { get; set; }
            public int TotalSucursales { get; set; }
            public decimal CoberturalPromedio { get; set; }
            public int TotalAsignacionesProductos { get; set; }
            public int TotalAsignacionesServicios { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(
            string tipo = "productos",
            string? categoria = null,
            string? marca = null,
            string? search = null,
            bool soloConAsignaciones = false,
            bool soloSinAsignaciones = false)
        {
            // Aplicar filtros
            TipoVista = tipo;
            CategoriaFilter = categoria;
            MarcaFilter = marca;
            SearchString = search;
            SoloConAsignaciones = soloConAsignaciones;
            SoloSinAsignaciones = soloSinAsignaciones;

            // Verificar permisos del usuario
            var userId = HttpContext.Session.GetString("UserId");
            if (!long.TryParse(userId, out var usuarioId))
            {
                return Challenge();
            }

            var tieneAccesoGlobal = await _sucursalFilter.TieneAccesoGlobalAsync(usuarioId);
            
            // Cargar sucursales disponibles
            if (tieneAccesoGlobal)
            {
                SucursalesDisponibles = await _context.Sucursals
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Name)
                    .ToListAsync();
            }
            else
            {
                var sucursalesIds = await _sucursalFilter.GetSucursalesUsuarioAsync(usuarioId);
                SucursalesDisponibles = await _context.Sucursals
                    .Where(s => s.IsActive && sucursalesIds.Contains(s.Id))
                    .OrderBy(s => s.Name)
                    .ToListAsync();
            }

            // Cargar datos según el tipo de vista
            if (TipoVista == "productos" || TipoVista == "ambos")
            {
                await CargarProductosMatrizAsync();
            }

            if (TipoVista == "servicios" || TipoVista == "ambos")
            {
                await CargarServiciosMatrizAsync();
            }

            // Calcular estadísticas
            CalcularEstadisticas();

            return Page();
        }

        public async Task<IActionResult> OnPostAsignacionRapidaAsync()
        {
            try
            {
                var accion = Request.Form["Accion"].FirstOrDefault(); // asignar, desasignar
                var tipo = Request.Form["Tipo"].FirstOrDefault(); // producto, servicio
                var itemIdStr = Request.Form["ItemId"].FirstOrDefault() ?? "0";
                var sucursalIdStr = Request.Form["SucursalId"].FirstOrDefault() ?? "0";

                // Debug: Log de parámetros recibidos
                System.Diagnostics.Debug.WriteLine($"?? AsignacionRapida INICIADA:");
                System.Diagnostics.Debug.WriteLine($"   ?? Acción: {accion}");
                System.Diagnostics.Debug.WriteLine($"   ?? Tipo: {tipo}");
                System.Diagnostics.Debug.WriteLine($"   ?? ItemId: {itemIdStr}");
                System.Diagnostics.Debug.WriteLine($"   ?? SucursalId: {sucursalIdStr}");

                // Validaciones básicas
                if (!long.TryParse(itemIdStr, out var itemId) || itemId == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"? ID de {tipo} inválido");
                    return new JsonResult(new { success = false, message = $"ID de {tipo} inválido: {itemIdStr}" });
                }

                if (!long.TryParse(sucursalIdStr, out var sucursalId) || sucursalId == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"? ID de sucursal inválido");
                    return new JsonResult(new { success = false, message = $"ID de sucursal inválido: {sucursalIdStr}" });
                }

                bool resultado = false;
                string mensaje = "";

                if (tipo == "producto")
                {
                    System.Diagnostics.Debug.WriteLine($"?? Procesando PRODUCTO...");
                    
                    // *** DEBUGGING: Verificar estado actual en BD ***
                    var asignacionActual = await _context.ProductoSucursales
                        .FirstOrDefaultAsync(ps => ps.ProductoId == itemId && ps.SucursalId == sucursalId);
                    
                    System.Diagnostics.Debug.WriteLine($"?? Estado ANTES del procesamiento:");
                    if (asignacionActual == null)
                    {
                        System.Diagnostics.Debug.WriteLine("   ? No existe asignación en BD");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"   ? Existe asignación: ID={asignacionActual.Id}, IsActive={asignacionActual.IsActive}, Disponible={asignacionActual.Disponible}");
                    }

                    resultado = await ProcesarAsignacionProductoAsync(itemId, sucursalId, accion == "asignar");
                    
                    System.Diagnostics.Debug.WriteLine($"?? Resultado del procesamiento: {resultado}");
                    
                    if (resultado)
                    {
                        System.Diagnostics.Debug.WriteLine($"?? Guardando cambios en BD...");
                        
                        try
                        {
                            var changesSaved = await _context.SaveChangesAsync();
                            System.Diagnostics.Debug.WriteLine($"? {changesSaved} cambios guardados en BD");
                            
                            mensaje = accion == "asignar" ? "Producto asignado exitosamente" : "Producto desasignado exitosamente";
                        }
                        catch (Exception saveEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"? ERROR al guardar cambios: {saveEx.Message}");
                            return new JsonResult(new { success = false, message = $"Error al guardar: {saveEx.Message}" });
                        }
                    }
                    else
                    {
                        // Mensaje más específico basado en el estado actual
                        if (accion == "asignar")
                        {
                            mensaje = asignacionActual != null && asignacionActual.IsActive 
                                ? "El producto ya está asignado a esta sucursal" 
                                : "No se pudo asignar el producto. Ver logs para detalles.";
                        }
                        else
                        {
                            mensaje = asignacionActual == null || !asignacionActual.IsActive
                                ? "El producto no está asignado a esta sucursal"
                                : "No se pudo desasignar el producto. Ver logs para detalles.";
                        }
                    }
                }
                else if (tipo == "servicio")
                {
                    System.Diagnostics.Debug.WriteLine($"?? Procesando SERVICIO...");
                    
                    resultado = await ProcesarAsignacionServicioAsync(itemId, sucursalId, accion == "asignar");
                    
                    if (resultado)
                    {
                        await _context.SaveChangesAsync();
                        mensaje = accion == "asignar" ? "Servicio asignado exitosamente" : "Servicio desasignado exitosamente";
                    }
                    else
                    {
                        mensaje = accion == "asignar" ? "El servicio ya está asignado a esta sucursal" : "El servicio no está asignado a esta sucursal";
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"? Tipo inválido: {tipo}");
                    return new JsonResult(new { success = false, message = $"Tipo inválido: '{tipo}'. Debe ser 'producto' o 'servicio'" });
                }

                System.Diagnostics.Debug.WriteLine($"?? RESULTADO FINAL: Success={resultado}, Mensaje='{mensaje}'");

                return new JsonResult(new { success = resultado, message = mensaje });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"?? ERROR GENERAL en OnPostAsignacionRapidaAsync: {ex}");
                return new JsonResult(new { success = false, message = $"Error interno: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostAsignacionMasivaAsync()
        {
            try
            {
                var accion = Request.Form["AccionMasiva"].FirstOrDefault(); // asignar, desasignar
                var tipo = Request.Form["TipoMasivo"].FirstOrDefault(); // productos, servicios
                var itemIds = Request.Form["ItemIds"].ToList().Select(id => long.TryParse(id, out var result) ? result : 0).Where(id => id > 0).ToList();
                var sucursalId = long.Parse(Request.Form["SucursalIdMasivo"].FirstOrDefault() ?? "0");

                var contador = 0;

                foreach (var itemId in itemIds)
                {
                    if (tipo == "productos")
                    {
                        if (await ProcesarAsignacionProductoAsync(itemId, sucursalId, accion == "asignar_todas"))
                            contador++;
                    }
                    else if (tipo == "servicios")
                    {
                        if (await ProcesarAsignacionServicioAsync(itemId, sucursalId, accion == "asignar_todas"))
                            contador++;
                    }
                }

                await _context.SaveChangesAsync();

                return new JsonResult(new { 
                    success = true, 
                    message = $"Se procesaron {contador} elementos exitosamente" 
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        private async Task CargarProductosMatrizAsync()
        {
            var query = _context.Productos.Where(p => p.IsActive);

            // Aplicar filtros
            if (!string.IsNullOrEmpty(CategoriaFilter))
                query = query.Where(p => p.Categoria == CategoriaFilter);

            if (!string.IsNullOrEmpty(MarcaFilter))
                query = query.Where(p => p.Marca == MarcaFilter);

            if (!string.IsNullOrEmpty(SearchString))
            {
                var search = SearchString.ToLower();
                query = query.Where(p => 
                    p.Nombre.ToLower().Contains(search) ||
                    p.Codigo.ToLower().Contains(search) ||
                    p.Marca.ToLower().Contains(search));
            }

            var productos = await query
                .Include(p => p.SucursalesAsignadas.Where(ps => ps.IsActive)) // *** AQUÍ ESTÁ EL PROBLEMA ***
                .ThenInclude(ps => ps.Sucursal)
                .OrderBy(p => p.Categoria)
                .ThenBy(p => p.Nombre)
                .ToListAsync();

            ProductosMatriz = productos.Select(p => {
                var asignaciones = p.SucursalesAsignadas.ToDictionary(ps => ps.SucursalId, ps => ps);
                var asignacionesPorSucursal = SucursalesDisponibles.ToDictionary(
                    s => s.Id, 
                    s => asignaciones.GetValueOrDefault(s.Id)
                );

                var totalAsignadas = asignaciones.Count;
                var precios = asignaciones.Values.Select(ps => ps.PrecioVenta).ToList();
                var precioPromedio = precios.Any() ? precios.Average() : p.PrecioBase;
                var rangoPrecio = precios.Any() && precios.Count > 1 
                    ? $"${precios.Min():F2} - ${precios.Max():F2}"
                    : precios.Any() ? $"${precios.First():F2}" : $"${p.PrecioBase:F2}";

                // *** DEBUG: Log de asignaciones por producto ***
                System.Diagnostics.Debug.WriteLine($"Producto {p.Nombre} (ID: {p.Id}): {totalAsignadas} asignaciones activas");
                foreach (var asig in asignaciones)
                {
                    System.Diagnostics.Debug.WriteLine($"  - Sucursal {asig.Key}: IsActive={asig.Value.IsActive}, Disponible={asig.Value.Disponible}");
                }

                return new ProductoMatriz
                {
                    Producto = p,
                    AsignacionesPorSucursal = asignacionesPorSucursal,
                    TotalSucursalesAsignadas = totalAsignadas,
                    PrecioPromedio = precioPromedio,
                    RangoPrecio = rangoPrecio
                };
            }).ToList();

            // Aplicar filtros de asignación
            if (SoloConAsignaciones)
                ProductosMatriz = ProductosMatriz.Where(p => p.TotalSucursalesAsignadas > 0).ToList();
            else if (SoloSinAsignaciones)
                ProductosMatriz = ProductosMatriz.Where(p => p.TotalSucursalesAsignadas == 0).ToList();
        }

        private async Task CargarServiciosMatrizAsync()
        {
            var query = _context.Servicios.Where(s => s.IsActive);

            // Aplicar filtros
            if (!string.IsNullOrEmpty(CategoriaFilter))
                query = query.Where(s => s.Categoria == CategoriaFilter);

            if (!string.IsNullOrEmpty(SearchString))
            {
                var search = SearchString.ToLower();
                query = query.Where(s => 
                    s.Nombre.ToLower().Contains(search) ||
                    s.Codigo.ToLower().Contains(search));
            }

            var servicios = await query
                .Include(s => s.SucursalesAsignadas.Where(ss => ss.IsActive))
                .ThenInclude(ss => ss.Sucursal)
                .OrderBy(s => s.Categoria)
                .ThenBy(s => s.Nombre)
                .ToListAsync();

            ServiciosMatriz = servicios.Select(s => {
                var asignaciones = s.SucursalesAsignadas.ToDictionary(ss => ss.SucursalId, ss => ss);
                var asignacionesPorSucursal = SucursalesDisponibles.ToDictionary(
                    suc => suc.Id, 
                    suc => asignaciones.GetValueOrDefault(suc.Id)
                );

                var totalAsignadas = asignaciones.Count;
                var precios = asignaciones.Values.Select(ss => ss.PrecioLocal).ToList();
                var precioPromedio = precios.Any() ? precios.Average() : s.PrecioBase;
                var rangoPrecio = precios.Any() && precios.Count > 1 
                    ? $"${precios.Min():F2} - ${precios.Max():F2}"
                    : precios.Any() ? $"${precios.First():F2}" : $"${s.PrecioBase:F2}";

                return new ServicioMatriz
                {
                    Servicio = s,
                    AsignacionesPorSucursal = asignacionesPorSucursal,
                    TotalSucursalesAsignadas = totalAsignadas,
                    PrecioPromedio = precioPromedio,
                    RangoPrecio = rangoPrecio
                };
            }).ToList();

            // Aplicar filtros de asignación
            if (SoloConAsignaciones)
                ServiciosMatriz = ServiciosMatriz.Where(s => s.TotalSucursalesAsignadas > 0).ToList();
            else if (SoloSinAsignaciones)
                ServiciosMatriz = ServiciosMatriz.Where(s => s.TotalSucursalesAsignadas == 0).ToList();
        }

        private void CalcularEstadisticas()
        {
            Estadisticas.TotalSucursales = SucursalesDisponibles.Count;

            if (ProductosMatriz.Any())
            {
                Estadisticas.TotalProductos = ProductosMatriz.Count;
                Estadisticas.ProductosConAsignaciones = ProductosMatriz.Count(p => p.TotalSucursalesAsignadas > 0);
                Estadisticas.ProductosSinAsignaciones = ProductosMatriz.Count(p => p.TotalSucursalesAsignadas == 0);
                Estadisticas.TotalAsignacionesProductos = ProductosMatriz.Sum(p => p.TotalSucursalesAsignadas);
            }

            if (ServiciosMatriz.Any())
            {
                Estadisticas.TotalServicios = ServiciosMatriz.Count;
                Estadisticas.ServiciosConAsignaciones = ServiciosMatriz.Count(s => s.TotalSucursalesAsignadas > 0);
                Estadisticas.ServiciosSinAsignaciones = ServiciosMatriz.Count(s => s.TotalSucursalesAsignadas == 0);
                Estadisticas.TotalAsignacionesServicios = ServiciosMatriz.Sum(s => s.TotalSucursalesAsignadas);
            }

            var totalItems = Estadisticas.TotalProductos + Estadisticas.TotalServicios;
            var totalConAsignaciones = Estadisticas.ProductosConAsignaciones + Estadisticas.ServiciosConAsignaciones;
            
            Estadisticas.CoberturalPromedio = totalItems > 0 
                ? (decimal)totalConAsignaciones / totalItems * 100 
                : 0;
        }

        private async Task<bool> ProcesarAsignacionProductoAsync(long productoId, long sucursalId, bool asignar)
        {
            try
            {
                var producto = await _context.Productos.FindAsync(productoId);
                if (producto == null) 
                {
                    System.Diagnostics.Debug.WriteLine($"? Producto {productoId} no encontrado");
                    return false;
                }

                var sucursal = await _context.Sucursals.FindAsync(sucursalId);
                if (sucursal == null)
                {
                    System.Diagnostics.Debug.WriteLine($"? Sucursal {sucursalId} no encontrada");
                    return false;
                }

                var asignacionExistente = await _context.ProductoSucursales
                    .FirstOrDefaultAsync(ps => ps.ProductoId == productoId && ps.SucursalId == sucursalId);

                System.Diagnostics.Debug.WriteLine($"?? Procesando Producto {productoId} - Sucursal {sucursalId}:");
                System.Diagnostics.Debug.WriteLine($"   ?? Producto: {producto.Nombre}");
                System.Diagnostics.Debug.WriteLine($"   ?? Sucursal: {sucursal.Name}");
                System.Diagnostics.Debug.WriteLine($"   ?? Acción: {(asignar ? "ASIGNAR" : "DESASIGNAR")}");
                System.Diagnostics.Debug.WriteLine($"   ?? Asignación existente: {(asignacionExistente != null ? $"SÍ (ID: {asignacionExistente.Id}, Active: {asignacionExistente.IsActive})" : "NO")}");

                if (asignar)
                {
                    // Queremos asignar el producto
                    if (asignacionExistente == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"? Creando nueva asignación...");
                        
                        try
                        {
                            // Validar que el producto esté activo
                            if (!producto.IsActive)
                            {
                                System.Diagnostics.Debug.WriteLine($"? El producto no está activo");
                                return false;
                            }

                            // Validar que la sucursal esté activa
                            if (!sucursal.IsActive)
                            {
                                System.Diagnostics.Debug.WriteLine($"? La sucursal no está activa");
                                return false;
                            }

                            // No existe asignación, crear nueva con todos los campos requeridos
                            var nuevaAsignacion = new ProductoSucursal
                            {
                                ProductoId = productoId,
                                SucursalId = sucursalId,
                                PrecioVenta = producto.PrecioBase,
                                PrecioMayoreo = producto.PrecioBase * 0.9m,
                                CantidadMayoreo = 10,
                                CostoLocal = producto.PrecioBase * 0.7m, // Costo estimado
                                StockActual = producto.RequiereInventario ? 10 : 0,
                                StockMinimoLocal = producto.RequiereInventario ? 5 : 0,
                                StockMaximoLocal = producto.RequiereInventario ? 50 : 0,
                                PuntoReordenLocal = producto.RequiereInventario ? 3 : 0,
                                Disponible = true,
                                SePuedeVender = true,
                                SePuedeReservar = true,
                                RequiereAutorizacion = false,
                                DescuentoMaximoLocal = 0.10m, // 10% descuento máximo
                                EnPromocion = false,
                                OrdenDisplay = 0,
                                EsDestacadoLocal = false,
                                TotalVendido = 0,
                                IngresosGenerados = 0.00m,
                                ConfiguracionLocal = "{}",
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            };
                            
                            System.Diagnostics.Debug.WriteLine($"??? Asignación creada en memoria con todos los campos");
                            
                            _context.ProductoSucursales.Add(nuevaAsignacion);
                            System.Diagnostics.Debug.WriteLine($"? Asignación agregada al contexto");
                            
                            return true;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"? Error al crear asignación: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"? StackTrace: {ex.StackTrace}");
                            throw;
                        }
                    }
                    else if (!asignacionExistente.IsActive)
                    {
                        System.Diagnostics.Debug.WriteLine($"?? Reactivando asignación existente...");
                        
                        // Existe pero está inactiva, reactivar
                        asignacionExistente.IsActive = true;
                        asignacionExistente.Disponible = true;
                        asignacionExistente.SePuedeVender = true;
                        asignacionExistente.UpdatedAt = DateTime.UtcNow;
                        System.Diagnostics.Debug.WriteLine($"?? Asignación reactivada");
                        return true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"?? El producto ya está asignado y activo");
                        // Ya está asignada y activa, no hacer nada
                        return false;
                    }
                }
                else
                {
                    // Queremos desasignar el producto
                    if (asignacionExistente != null && asignacionExistente.IsActive)
                    {
                        System.Diagnostics.Debug.WriteLine($"?? Desactivando asignación...");
                        
                        asignacionExistente.IsActive = false;
                        asignacionExistente.Disponible = false;
                        asignacionExistente.UpdatedAt = DateTime.UtcNow;
                        System.Diagnostics.Debug.WriteLine($"?? Asignación desactivada");
                        return true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"?? No hay asignación activa para desactivar");
                        // No está asignada o ya está inactiva, no hacer nada
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log del error para debugging
                System.Diagnostics.Debug.WriteLine($"? ERROR GENERAL en ProcesarAsignacionProductoAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"? StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task<bool> ProcesarAsignacionServicioAsync(long servicioId, long sucursalId, bool asignar)
        {
            try
            {
                var servicio = await _context.Servicios.FindAsync(servicioId);
                if (servicio == null) 
                {
                    return false;
                }

                var asignacionExistente = await _context.ServicioSucursales
                    .FirstOrDefaultAsync(ss => ss.ServicioId == servicioId && ss.SucursalId == sucursalId);

                if (asignar)
                {
                    // Queremos asignar el servicio
                    if (asignacionExistente == null)
                    {
                        // No existe asignación, crear nueva
                        var nuevaAsignacion = new ServicioSucursal
                        {
                            ServicioId = servicioId,
                            SucursalId = sucursalId,
                            PrecioLocal = servicio.PrecioBase,
                            DuracionLocal = servicio.DuracionEstimada,
                            Disponible = true,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.ServicioSucursales.Add(nuevaAsignacion);
                        return true;
                    }
                    else if (!asignacionExistente.IsActive)
                    {
                        // Existe pero está inactiva, reactivar
                        asignacionExistente.IsActive = true;
                        asignacionExistente.Disponible = true;
                        asignacionExistente.UpdatedAt = DateTime.UtcNow;
                        return true;
                    }
                    else
                    {
                        // Ya está asignado y activo, no hacer nada
                        return false;
                    }
                }
                else
                {
                    // Queremos desasignar el servicio
                    if (asignacionExistente != null && asignacionExistente.IsActive)
                    {
                        asignacionExistente.IsActive = false;
                        asignacionExistente.Disponible = false;
                        asignacionExistente.UpdatedAt = DateTime.UtcNow;
                        return true;
                    }
                    else
                    {
                        // No está asignado o ya está inactivo, no hacer nada
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log del error para debugging
                System.Diagnostics.Debug.WriteLine($"Error en ProcesarAsignacionServicioAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<IActionResult> OnPostDiagnosticoAsync()
        {
            try
            {
                var productoId = long.Parse(Request.Form["ProductoId"].FirstOrDefault() ?? "0");
                var sucursalId = long.Parse(Request.Form["SucursalId"].FirstOrDefault() ?? "0");

                // Buscar todas las asignaciones para este producto-sucursal (activas e inactivas)
                var todasAsignaciones = await _context.ProductoSucursales
                    .Where(ps => ps.ProductoId == productoId && ps.SucursalId == sucursalId)
                    .Include(ps => ps.Producto)
                    .Include(ps => ps.Sucursal)
                    .ToListAsync();

                var diagnostico = new
                {
                    productoId = productoId,
                    sucursalId = sucursalId,
                    totalAsignaciones = todasAsignaciones.Count,
                    asignaciones = todasAsignaciones.Select(a => new
                    {
                        id = a.Id,
                        isActive = a.IsActive,
                        disponible = a.Disponible,
                        precio = a.PrecioVenta,
                        createdAt = a.CreatedAt,
                        updatedAt = a.UpdatedAt
                    }).ToList()
                };

                return new JsonResult(new { success = true, diagnostico = diagnostico });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
    }
}