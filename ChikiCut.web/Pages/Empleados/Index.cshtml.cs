using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Services;

namespace ChikiCut.web.Pages.Empleados
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ISucursalFilterService _sucursalFilter;

        public IndexModel(AppDbContext context, ISucursalFilterService sucursalFilter)
        {
            _context = context;
            _sucursalFilter = sucursalFilter;
        }

        public IList<Empleado> Empleados { get; set; } = default!;
        public int TotalEmpleados { get; set; }
        public int EmpleadosActivos { get; set; }
        public List<string> SucursalesUsuario { get; set; } = new();
        public bool TieneAccesoGlobal { get; set; }

        // Propiedades para filtros dinámicos
        public string? SearchString { get; set; }
        public string? SucursalFilter { get; set; }
        public string? PuestoFilter { get; set; }
        public bool? SoloActivos { get; set; }
        public List<string> SucursalesDisponibles { get; set; } = new();
        public List<string> PuestosDisponibles { get; set; } = new();

        public async Task OnGetAsync(string? searchString, string? sucursalFilter, string? puestoFilter, bool? soloActivos)
        {
            SearchString = searchString;
            SucursalFilter = sucursalFilter;
            PuestoFilter = puestoFilter;
            SoloActivos = soloActivos;

            // Obtener ID del usuario actual
            var userId = HttpContext.Session.GetString("UserId");
            if (!long.TryParse(userId, out var usuarioId))
            {
                Empleados = new List<Empleado>();
                return;
            }

            // Verificar si tiene acceso global
            TieneAccesoGlobal = await _sucursalFilter.TieneAccesoGlobalAsync(usuarioId);

            // Obtener sucursales del usuario para mostrar en la interfaz
            var sucursalesIds = await _sucursalFilter.GetSucursalesUsuarioAsync(usuarioId);
            SucursalesUsuario = await _context.Sucursals
                .Where(s => sucursalesIds.Contains(s.Id))
                .Select(s => s.Name)
                .OrderBy(name => name)
                .ToListAsync();

            // Aplicar filtro por sucursal
            var query = _context.Empleados
                .Include(e => e.Sucursal)
                .Include(e => e.PuestoNavegacion)
                .AsQueryable();

            // FILTRADO AUTOMÁTICO POR SUCURSAL
            var empleadosFiltrados = await _sucursalFilter
                .FiltrarPorSucursalAsync(query, usuarioId);

            // Cargar filtros dinámicos
            await CargarFiltrosDinamicosAsync(empleadosFiltrados);

            // Aplicar filtros adicionales
            if (!string.IsNullOrEmpty(SearchString))
            {
                empleadosFiltrados = empleadosFiltrados.Where(e => 
                    EF.Functions.ILike(e.CodigoEmpleado, $"%{SearchString}%") ||
                    EF.Functions.ILike(e.Nombre, $"%{SearchString}%") ||
                    EF.Functions.ILike(e.ApellidoPaterno, $"%{SearchString}%") ||
                    (e.ApellidoMaterno != null && EF.Functions.ILike(e.ApellidoMaterno, $"%{SearchString}%")) ||
                    (e.Email != null && EF.Functions.ILike(e.Email, $"%{SearchString}%")));
            }

            if (!string.IsNullOrEmpty(SucursalFilter))
            {
                empleadosFiltrados = empleadosFiltrados.Where(e => e.Sucursal.Name == SucursalFilter);
            }

            if (!string.IsNullOrEmpty(PuestoFilter))
            {
                empleadosFiltrados = empleadosFiltrados.Where(e => e.PuestoNavegacion.Nombre == PuestoFilter);
            }

            if (SoloActivos.HasValue)
            {
                empleadosFiltrados = empleadosFiltrados.Where(e => e.IsActive == SoloActivos.Value);
            }

            Empleados = await empleadosFiltrados
                .OrderBy(e => e.Sucursal.Name)
                .ThenBy(e => e.ApellidoPaterno)
                .ToListAsync();

            TotalEmpleados = Empleados.Count;
            EmpleadosActivos = Empleados.Count(e => e.IsActive);
        }

        private async Task CargarFiltrosDinamicosAsync(IQueryable<Empleado> baseQuery)
        {
            // Cargar sucursales disponibles (basadas en los empleados filtrados)
            SucursalesDisponibles = await baseQuery
                .Where(e => e.Sucursal != null)
                .Select(e => e.Sucursal.Name)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            // Cargar puestos disponibles (basados en los empleados filtrados)
            PuestosDisponibles = await baseQuery
                .Where(e => e.PuestoNavegacion != null)
                .Select(e => e.PuestoNavegacion.Nombre)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();
        }
    }
}