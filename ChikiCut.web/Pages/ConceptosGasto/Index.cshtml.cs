using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;

namespace ChikiCut.web.Pages.ConceptosGasto
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<ConceptoGasto> ConceptosGasto { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? CategoriaFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SoloActivos { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.ConceptosGasto
                .Include(c => c.UsuarioCreador)
                .AsQueryable();

            // Filtrar por activos/inactivos
            if (!string.IsNullOrEmpty(SoloActivos))
            {
                if (bool.TryParse(SoloActivos, out bool isActive))
                {
                    query = query.Where(c => c.IsActive == isActive);
                }
            }

            // Filtrar por categoría
            if (!string.IsNullOrEmpty(CategoriaFilter))
            {
                query = query.Where(c => c.Categoria == CategoriaFilter);
            }

            // Filtrar por búsqueda - solo en campos de texto para evitar problemas con JSONB
            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(c => 
                    c.Codigo.Contains(SearchString) ||
                    c.Nombre.Contains(SearchString) ||
                    (c.Descripcion != null && c.Descripcion.Contains(SearchString)) ||
                    (c.Subcategoria != null && c.Subcategoria.Contains(SearchString)) ||
                    (c.CuentaContable != null && c.CuentaContable.Contains(SearchString)));
            }

            // Ejecutar consulta
            var conceptos = await query
                .OrderBy(c => c.Categoria)
                .ThenBy(c => c.Codigo)
                .ToListAsync();

            // Si hay búsqueda, también filtrar por tags en memoria
            if (!string.IsNullOrEmpty(SearchString))
            {
                var searchLower = SearchString.ToLower();
                var conceptosConTags = conceptos.Where(c => 
                    c.TagsList.Any(tag => tag.ToLower().Contains(searchLower))
                ).ToList();

                // Combinar resultados y eliminar duplicados
                ConceptosGasto = conceptos.Union(conceptosConTags)
                    .DistinctBy(c => c.Id)
                    .OrderBy(c => c.Categoria)
                    .ThenBy(c => c.Codigo)
                    .ToList();
            }
            else
            {
                ConceptosGasto = conceptos;
            }
        }
    }
}