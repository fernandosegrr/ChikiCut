using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Attributes;

namespace ChikiCut.web.Pages.Servicios
{
    [RequirePermission("servicios", "read")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context) => _context = context;

        public IList<Servicio> Servicios { get; set; } = default!;
        public string? SearchString { get; set; }
        public string? CategoriaFilter { get; set; }
        public bool? SoloActivos { get; set; }
        public int TotalServicios { get; set; }
        public int ServiciosActivos { get; set; }
        public decimal PrecioPromedio { get; set; }

        // Propiedades para filtros dinámicos
        public List<string> CategoriasDisponibles { get; set; } = new();

        public async Task OnGetAsync(string? searchString, string? categoriaFilter, bool? soloActivos)
        {
            SearchString = searchString;
            CategoriaFilter = categoriaFilter;
            SoloActivos = soloActivos;

            // Cargar filtros dinámicos primero
            await CargarFiltrosDinamicosAsync();

            var query = _context.Servicios
                .Include(s => s.SucursalesAsignadas.Where(ss => ss.IsActive))
                .AsQueryable();

            // Filtros
            if (!string.IsNullOrEmpty(SearchString))
            {
                var searchLower = SearchString.ToLower();
                query = query.Where(s => EF.Functions.ILike(s.Codigo, $"%{SearchString}%") ||
                                        EF.Functions.ILike(s.Nombre, $"%{SearchString}%") ||
                                        (s.Descripcion != null && EF.Functions.ILike(s.Descripcion, $"%{SearchString}%")) ||
                                        EF.Functions.ILike(s.Categoria, $"%{SearchString}%"));
            }

            if (!string.IsNullOrEmpty(CategoriaFilter))
            {
                query = query.Where(s => s.Categoria == CategoriaFilter);
            }

            if (SoloActivos.HasValue)
            {
                query = query.Where(s => s.IsActive == SoloActivos.Value);
            }

            Servicios = await query
                .OrderBy(s => s.Categoria)
                .ThenBy(s => s.Nombre)
                .ToListAsync();

            // Estadísticas
            TotalServicios = Servicios.Count;
            ServiciosActivos = Servicios.Count(s => s.IsActive);
            PrecioPromedio = Servicios.Any() ? Servicios.Average(s => s.PrecioBase) : 0;
        }

        private async Task CargarFiltrosDinamicosAsync()
        {
            // Cargar categorías distintas que existen en la base de datos
            CategoriasDisponibles = await _context.Servicios
                .Where(s => s.IsActive && !string.IsNullOrEmpty(s.Categoria))
                .Select(s => s.Categoria)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        [RequirePermission("servicios", "update")]
        public async Task<IActionResult> OnPostToggleEstadoAsync(long id)
        {
            try
            {
                var servicio = await _context.Servicios.FindAsync(id);
                if (servicio == null)
                {
                    return new JsonResult(new { success = false, error = "Servicio no encontrado" });
                }

                // Cambiar estado
                servicio.IsActive = !servicio.IsActive;
                servicio.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var estado = servicio.IsActive ? "activado" : "inactivado";
                TempData["SuccessMessage"] = $"Servicio '{servicio.Nombre}' {estado} exitosamente.";
                
                return new JsonResult(new { 
                    success = true, 
                    newState = servicio.IsActive,
                    message = $"Servicio {estado} correctamente"
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