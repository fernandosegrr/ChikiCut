using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Attributes;

namespace ChikiCut.web.Pages.Productos
{
    [RequirePermission("productos", "read")]
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;

        public DetailsModel(AppDbContext context) => _context = context;

        public Producto Producto { get; set; } = default!;
        public List<ProductoSucursal> SucursalesAsignadas { get; set; } = new();
        public List<Sucursal> SucursalesDisponibles { get; set; } = new();
        public Dictionary<string, object> EstadisticasInventario { get; set; } = new();

        // Propiedades calculadas para f�cil acceso en la vista
        public int StockTotal { get; set; }
        public int SucursalesStockBajo { get; set; }
        public decimal ValorTotalInventario { get; set; }
        public decimal PrecioPromedio { get; set; }
        public int TotalVendido { get; set; }
        public decimal IngresosGenerados { get; set; }
        public string SucursalMasStock { get; set; } = "";
        public string SucursalMenosStock { get; set; } = "";

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos
                .Include(p => p.UsuarioCreador)
                .Include(p => p.ProveedorPrincipal)
                .Include(p => p.SucursalesAsignadas.Where(ps => ps.IsActive))
                    .ThenInclude(ps => ps.Sucursal)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (producto == null)
            {
                return NotFound();
            }

            Producto = producto;
            SucursalesAsignadas = producto.SucursalesAsignadas.ToList();

            // Calcular estad�sticas de inventario
            await CalcularEstadisticasInventarioAsync();

            // Cargar sucursales disponibles para asignaci�n
            await CargarSucursalesDisponiblesAsync();

            return Page();
        }

        private async Task CalcularEstadisticasInventarioAsync()
        {
            EstadisticasInventario = new Dictionary<string, object>();

            if (!Producto.RequiereInventario)
            {
                EstadisticasInventario["RequiereInventario"] = false;
                StockTotal = 0;
                SucursalesStockBajo = 0;
                ValorTotalInventario = 0;
                PrecioPromedio = Producto.PrecioBase;
                TotalVendido = 0;
                IngresosGenerados = 0;
                SucursalMasStock = "N/A";
                SucursalMenosStock = "N/A";
                return;
            }

            EstadisticasInventario["RequiereInventario"] = true;

            // Stock total en todas las sucursales
            StockTotal = SucursalesAsignadas.Sum(ps => ps.StockActual);
            EstadisticasInventario["StockTotal"] = StockTotal;

            // Sucursales con stock bajo
            SucursalesStockBajo = SucursalesAsignadas
                .Where(ps => ps.StockActual <= (ps.StockMinimoLocal ?? Producto.StockMinimo))
                .Count();
            EstadisticasInventario["SucursalesStockBajo"] = SucursalesStockBajo;

            // Sucursal con m�s stock
            var sucursalMasStock = SucursalesAsignadas
                .OrderByDescending(ps => ps.StockActual)
                .FirstOrDefault();
            SucursalMasStock = sucursalMasStock?.Sucursal.Name ?? "N/A";
            EstadisticasInventario["SucursalMasStock"] = SucursalMasStock;
            EstadisticasInventario["StockMayor"] = sucursalMasStock?.StockActual ?? 0;

            // Sucursal con menos stock
            var sucursalMenosStock = SucursalesAsignadas
                .OrderBy(ps => ps.StockActual)
                .FirstOrDefault();
            SucursalMenosStock = sucursalMenosStock?.Sucursal.Name ?? "N/A";
            EstadisticasInventario["SucursalMenosStock"] = SucursalMenosStock;
            EstadisticasInventario["StockMenor"] = sucursalMenosStock?.StockActual ?? 0;

            // Valor total del inventario
            ValorTotalInventario = SucursalesAsignadas.Sum(ps => ps.StockActual * ps.PrecioVenta);
            EstadisticasInventario["ValorTotalInventario"] = ValorTotalInventario;

            // Promedio de precio de venta
            PrecioPromedio = SucursalesAsignadas.Any() 
                ? SucursalesAsignadas.Average(ps => ps.PrecioVenta) 
                : Producto.PrecioBase;
            EstadisticasInventario["PrecioPromedio"] = PrecioPromedio;

            // Variaci�n de precios
            if (SucursalesAsignadas.Any())
            {
                var precioMin = SucursalesAsignadas.Min(ps => ps.PrecioVenta);
                var precioMax = SucursalesAsignadas.Max(ps => ps.PrecioVenta);
                EstadisticasInventario["PrecioMinimo"] = precioMin;
                EstadisticasInventario["PrecioMaximo"] = precioMax;
                EstadisticasInventario["VariacionPrecios"] = precioMax - precioMin;
            }

            // Total vendido hist�rico (simulado - necesitar�as una tabla de ventas real)
            TotalVendido = SucursalesAsignadas.Sum(ps => 
                Math.Max(0, (ps.StockMinimoLocal ?? Producto.StockMinimo) - ps.StockActual + 50)); // Simulaci�n
            EstadisticasInventario["TotalVendido"] = TotalVendido;

            // Ingresos generados (estimaci�n basada en ventas simuladas)
            IngresosGenerados = TotalVendido * PrecioPromedio;
            EstadisticasInventario["IngresosGenerados"] = IngresosGenerados;

            // Rotaci�n estimada (ventas vs stock actual)
            if (StockTotal > 0 && TotalVendido > 0)
            {
                var rotacion = (double)TotalVendido / StockTotal;
                EstadisticasInventario["RotacionEstimada"] = rotacion;
            }
            else
            {
                EstadisticasInventario["RotacionEstimada"] = 0.0;
            }
        }

        private async Task CargarSucursalesDisponiblesAsync()
        {
            // Obtener todas las sucursales activas
            var todasLasSucursales = await _context.Sucursals
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            // Obtener IDs de sucursales ya asignadas
            var sucursalesAsignadasIds = SucursalesAsignadas.Select(ps => ps.SucursalId).ToHashSet();

            // Filtrar sucursales no asignadas
            SucursalesDisponibles = todasLasSucursales
                .Where(s => !sucursalesAsignadasIds.Contains(s.Id))
                .ToList();
        }

        public async Task<IActionResult> OnPostAsignarSucursalesRapidoAsync()
        {
            try
            {
                // Debug logging
                Console.WriteLine("?? Handler OnPostAsignarSucursalesRapidoAsync ejecut�ndose...");
                
                var productoId = Request.Form["ProductoId"].FirstOrDefault();
                var sucursalesSeleccionadas = Request.Form["SucursalesSeleccionadas"].ToList();
                var precioVentaStr = Request.Form["PrecioVenta"].FirstOrDefault();
                var stockInicialStr = Request.Form["StockInicial"].FirstOrDefault();

                Console.WriteLine($"?? Datos recibidos:");
                Console.WriteLine($"  ProductoId: {productoId}");
                Console.WriteLine($"  Sucursales: [{string.Join(", ", sucursalesSeleccionadas)}]");
                Console.WriteLine($"  PrecioVenta: {precioVentaStr}");
                Console.WriteLine($"  StockInicial: {stockInicialStr}");

                if (!long.TryParse(productoId, out var prodId))
                {
                    Console.WriteLine("? ProductoId inv�lido");
                    return new JsonResult(new { success = false, message = "ID de producto inv�lido" });
                }

                if (sucursalesSeleccionadas.Count == 0)
                {
                    Console.WriteLine("? No hay sucursales seleccionadas");
                    return new JsonResult(new { success = false, message = "No se seleccionaron sucursales" });
                }

                if (!decimal.TryParse(precioVentaStr, out var precioVenta) || precioVenta <= 0)
                {
                    Console.WriteLine("? Precio de venta inv�lido");
                    return new JsonResult(new { success = false, message = "Precio de venta inv�lido" });
                }

                if (!int.TryParse(stockInicialStr ?? "0", out var stockInicial) || stockInicial < 0)
                {
                    Console.WriteLine("? Stock inicial inv�lido");
                    return new JsonResult(new { success = false, message = "Stock inicial inv�lido" });
                }

                var producto = await _context.Productos.FindAsync(prodId);
                if (producto == null)
                {
                    Console.WriteLine($"? Producto {prodId} no encontrado");
                    return new JsonResult(new { success = false, message = "Producto no encontrado" });
                }

                Console.WriteLine($"? Producto encontrado: {producto.Nombre}");

                var sucursalesAsignadasCount = 0;

                foreach (var sucursalIdStr in sucursalesSeleccionadas)
                {
                    if (long.TryParse(sucursalIdStr, out var sucursalId))
                    {
                        Console.WriteLine($"?? Procesando sucursal ID: {sucursalId}");
                        
                        // Verificar si ya existe una asignaci�n
                        var asignacionExistente = await _context.ProductoSucursales
                            .FirstOrDefaultAsync(ps => ps.ProductoId == prodId && ps.SucursalId == sucursalId);

                        if (asignacionExistente == null)
                        {
                            Console.WriteLine($"? Creando nueva asignaci�n para sucursal {sucursalId}");
                            
                            // Crear nueva asignaci�n
                            var nuevaAsignacion = new ProductoSucursal
                            {
                                ProductoId = prodId,
                                SucursalId = sucursalId,
                                PrecioVenta = precioVenta,
                                PrecioMayoreo = precioVenta * 0.9m, // 10% descuento para mayoreo
                                CantidadMayoreo = 10,
                                StockActual = producto.RequiereInventario ? stockInicial : 0,
                                StockMinimoLocal = producto.RequiereInventario ? producto.StockMinimo : null,
                                StockMaximoLocal = producto.RequiereInventario ? producto.StockMaximo : null,
                                PuntoReordenLocal = producto.RequiereInventario ? producto.PuntoReorden : null,
                                Disponible = true,
                                SePuedeVender = true,
                                SePuedeReservar = true,
                                DescuentoMaximoLocal = producto.DescuentoMaximo,
                                EsDestacadoLocal = false,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            };

                            _context.ProductoSucursales.Add(nuevaAsignacion);
                            sucursalesAsignadasCount++;
                        }
                        else if (!asignacionExistente.IsActive)
                        {
                            Console.WriteLine($"?? Reactivando asignaci�n existente para sucursal {sucursalId}");
                            
                            // Reactivar asignaci�n existente pero inactiva
                            asignacionExistente.IsActive = true;
                            asignacionExistente.PrecioVenta = precioVenta;
                            asignacionExistente.StockActual = producto.RequiereInventario ? stockInicial : 0;
                            asignacionExistente.Disponible = true;
                            asignacionExistente.UpdatedAt = DateTime.UtcNow;
                            sucursalesAsignadasCount++;
                        }
                        else
                        {
                            Console.WriteLine($"?? Asignaci�n ya existe y est� activa para sucursal {sucursalId}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"? ID de sucursal inv�lido: {sucursalIdStr}");
                    }
                }

                Console.WriteLine($"?? Guardando cambios en la base de datos...");
                await _context.SaveChangesAsync();
                Console.WriteLine($"? Cambios guardados exitosamente. Sucursales asignadas: {sucursalesAsignadasCount}");

                return new JsonResult(new { 
                    success = true, 
                    message = $"Producto asignado exitosamente a {sucursalesAsignadasCount} sucursal(es)",
                    sucursalesAsignadas = sucursalesAsignadasCount
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? Error en OnPostAsignarSucursalesRapidoAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return new JsonResult(new { 
                    success = false, 
                    message = "Error interno del servidor: " + ex.Message,
                    details = ex.StackTrace
                });
            }
        }
    }
}