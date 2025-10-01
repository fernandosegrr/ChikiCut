using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Attributes;
using ChikiCut.web.Helpers;

namespace ChikiCut.web.Pages.Proveedores
{
    [RequirePermission("proveedores", "read")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly PermissionHelper _permissionHelper;

        public IndexModel(AppDbContext context, PermissionHelper permissionHelper)
        {
            _context = context;
            _permissionHelper = permissionHelper;
        }

        public IList<Proveedor> Proveedores { get; set; } = default!;
        public int TotalProveedores { get; set; }
        public int ProveedoresActivos { get; set; }
        public string? BusquedaNombre { get; set; }
        public string? FiltroCategoria { get; set; }
        public bool? FiltroActivo { get; set; }
        public int? FiltroCalificacion { get; set; }
        public List<string> Categorias { get; set; } = new();
        
        // Propiedades de permisos
        public bool CanCreateProveedores { get; set; }
        public bool CanUpdateProveedores { get; set; }
        public bool CanDeleteProveedores { get; set; }

        public async Task OnGetAsync(string? busquedaNombre, string? filtroCategoria, bool? filtroActivo, int? filtroCalificacion)
        {
            BusquedaNombre = busquedaNombre;
            FiltroCategoria = filtroCategoria;
            FiltroActivo = filtroActivo;
            FiltroCalificacion = filtroCalificacion;

            // Verificar permisos usando PermissionHelper
            CanCreateProveedores = _permissionHelper.CanManageProviders();
            CanUpdateProveedores = _permissionHelper.CanEditProviders();
            CanDeleteProveedores = _permissionHelper.CanDeleteProviders();

            IQueryable<Proveedor> query = _context.Proveedores;

            // Aplicar filtros
            if (!string.IsNullOrEmpty(busquedaNombre))
            {
                query = query.Where(p => p.NombreComercial.Contains(busquedaNombre) || 
                                   (p.RazonSocial != null && p.RazonSocial.Contains(busquedaNombre)) ||
                                   (p.Rfc != null && p.Rfc.Contains(busquedaNombre)) ||
                                   (p.CodigoProveedor != null && p.CodigoProveedor.Contains(busquedaNombre)));
            }

            if (!string.IsNullOrEmpty(filtroCategoria))
            {
                query = query.Where(p => p.Categoria == filtroCategoria);
            }

            if (filtroActivo.HasValue)
            {
                query = query.Where(p => p.IsActive == filtroActivo.Value);
            }

            if (filtroCalificacion.HasValue)
            {
                query = query.Where(p => p.Calificacion == filtroCalificacion.Value);
            }

            Proveedores = await query
                .OrderBy(p => p.NombreComercial)
                .ToListAsync();

            // Obtener estadísticas
            TotalProveedores = await _context.Proveedores.CountAsync();
            ProveedoresActivos = await _context.Proveedores.CountAsync(p => p.IsActive);

            // Obtener categorías únicas para el filtro
            Categorias = await _context.Proveedores
                .Where(p => !string.IsNullOrEmpty(p.Categoria))
                .Select(p => p.Categoria)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }
    }
}