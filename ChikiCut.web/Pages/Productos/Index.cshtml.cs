using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Attributes;

namespace ChikiCut.web.Pages.Productos
{
    [RequirePermission("productos", "read")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context) => _context = context;

        public IList<Producto> Productos { get; set; } = default!;
        public string? SearchString { get; set; }
        public string? CategoriaFilter { get; set; }
        public string? MarcaFilter { get; set; }
        public bool? SoloActivos { get; set; }
        public bool? RequiereInventario { get; set; }
        public bool? SoloDestacados { get; set; }
        public int TotalProductos { get; set; }
        public int ProductosActivos { get; set; }
        public int ProductosStockBajo { get; set; }
        public decimal PrecioPromedio { get; set; }

        // Propiedades para filtros dinámicos
        public List<string> CategoriasDisponibles { get; set; } = new();
        public List<string> MarcasDisponibles { get; set; } = new();

        public async Task OnGetAsync(string? searchString, string? categoriaFilter, string? marcaFilter, bool? soloActivos, bool? requiereInventario, bool? soloDestacados)
        {
            SearchString = searchString;
            CategoriaFilter = categoriaFilter;
            MarcaFilter = marcaFilter;
            SoloActivos = soloActivos;
            RequiereInventario = requiereInventario;
            SoloDestacados = soloDestacados;

            // Cargar filtros dinámicos primero
            await CargarFiltrosDinamicosAsync();

            var query = _context.Productos
                .Include(p => p.SucursalesAsignadas.Where(ps => ps.IsActive))
                .AsQueryable();

            // Aplicar filtros
            if (!string.IsNullOrEmpty(SearchString))
            {
                var searchLower = SearchString.ToLower();
                query = query.Where(p => EF.Functions.ILike(p.Codigo, $"%{SearchString}%") ||
                                        EF.Functions.ILike(p.Nombre, $"%{SearchString}%") ||
                                        (p.Descripcion != null && EF.Functions.ILike(p.Descripcion, $"%{SearchString}%")) ||
                                        EF.Functions.ILike(p.Marca, $"%{SearchString}%") ||
                                        EF.Functions.ILike(p.Categoria, $"%{SearchString}%"));
            }

            if (!string.IsNullOrEmpty(CategoriaFilter))
            {
                query = query.Where(p => p.Categoria == CategoriaFilter);
            }

            if (!string.IsNullOrEmpty(MarcaFilter))
            {
                query = query.Where(p => p.Marca == MarcaFilter);
            }

            if (SoloActivos.HasValue)
            {
                query = query.Where(p => p.IsActive == SoloActivos.Value);
            }

            if (RequiereInventario.HasValue)
            {
                query = query.Where(p => p.RequiereInventario == RequiereInventario.Value);
            }

            if (SoloDestacados.HasValue && SoloDestacados.Value)
            {
                query = query.Where(p => p.SucursalesAsignadas.Any(ps => ps.EsDestacadoLocal));
            }

            Productos = await query
                .OrderBy(p => p.Categoria)
                .ThenBy(p => p.Nombre)
                .ToListAsync();

            // Estadísticas
            TotalProductos = Productos.Count;
            ProductosActivos = Productos.Count(p => p.IsActive);
            ProductosStockBajo = Productos.Count(p => p.RequiereInventario && p.SucursalesAsignadas.Any(ps => ps.StockActual <= (ps.StockMinimoLocal ?? p.StockMinimo)));
            PrecioPromedio = Productos.Any() ? Productos.Average(p => p.PrecioBase) : 0;
        }

        private async Task CargarFiltrosDinamicosAsync()
        {
            // Cargar categorías distintas que existen en la base de datos
            CategoriasDisponibles = await _context.Productos
                .Where(p => p.IsActive && !string.IsNullOrEmpty(p.Categoria))
                .Select(p => p.Categoria)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            // Cargar marcas distintas que existen en la base de datos
            MarcasDisponibles = await _context.Productos
                .Where(p => p.IsActive && !string.IsNullOrEmpty(p.Marca))
                .Select(p => p.Marca)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();
        }

        [RequirePermission("productos", "update")]
        public async Task<IActionResult> OnPostToggleEstadoAsync(long id)
        {
            try
            {
                var producto = await _context.Productos.FindAsync(id);
                if (producto == null)
                {
                    return new JsonResult(new { success = false, error = "Producto no encontrado" });
                }

                // Cambiar estado
                producto.IsActive = !producto.IsActive;
                producto.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var estado = producto.IsActive ? "activado" : "inactivado";
                TempData["SuccessMessage"] = $"Producto '{producto.Nombre}' {estado} exitosamente.";
                
                return new JsonResult(new { 
                    success = true, 
                    newState = producto.IsActive,
                    message = $"Producto {estado} correctamente"
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { 
                    success = false, 
                    error = ex.Message 
                });
            }
        }
    }
}