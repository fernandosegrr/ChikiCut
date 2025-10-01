using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Attributes;
using ChikiCut.web.Helpers;
using System.Text.Json;

namespace ChikiCut.web.Pages.Roles
{
    [RequirePermission("roles", "read")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly PermissionHelper _permissionHelper;

        public IndexModel(AppDbContext context, PermissionHelper permissionHelper)
        {
            _context = context;
            _permissionHelper = permissionHelper;
        }

        public IList<RolConDetalles> Roles { get; set; } = default!;
        public int TotalRoles { get; set; }
        public int RolesActivos { get; set; }
        public string? BusquedaNombre { get; set; }
        public int? FiltroNivel { get; set; }
        public bool? FiltroActivo { get; set; }
        public bool CanCreateRoles { get; set; }

        public class RolConDetalles
        {
            public long Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string? Descripcion { get; set; }
            public int NivelAcceso { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public int CantidadUsuarios { get; set; }
            public string NivelDescripcion => NivelAcceso switch
            {
                1 => "Limitado",
                2 => "Básico", 
                3 => "Intermedio",
                4 => "Avanzado",
                5 => "Administrador",
                _ => "Personalizado"
            };
            public string PermisosResumen { get; set; } = string.Empty;
        }

        public async Task OnGetAsync(string? busquedaNombre, int? filtroNivel, bool? filtroActivo)
        {
            BusquedaNombre = busquedaNombre;
            FiltroNivel = filtroNivel;
            FiltroActivo = filtroActivo;

            // Verificar permisos usando PermissionHelper
            CanCreateRoles = _permissionHelper.CanCreateRoles();

            IQueryable<Rol> query = _context.Roles;

            // Aplicar filtros
            if (!string.IsNullOrEmpty(busquedaNombre))
            {
                query = query.Where(r => r.Nombre.Contains(busquedaNombre) || 
                                   (r.Descripcion != null && r.Descripcion.Contains(busquedaNombre)));
            }

            if (filtroNivel.HasValue)
            {
                query = query.Where(r => r.NivelAcceso == filtroNivel.Value);
            }

            if (filtroActivo.HasValue)
            {
                query = query.Where(r => r.IsActive == filtroActivo.Value);
            }

            var rolesData = await query
                .Include(r => r.Usuarios)
                .OrderBy(r => r.NivelAcceso)
                .ThenBy(r => r.Nombre)
                .ToListAsync();

            Roles = rolesData.Select(r => new RolConDetalles
            {
                Id = r.Id,
                Nombre = r.Nombre,
                Descripcion = r.Descripcion,
                NivelAcceso = r.NivelAcceso,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt,
                CantidadUsuarios = r.Usuarios?.Count(u => u.IsActive) ?? 0,
                PermisosResumen = ObtenerResumenPermisos(r.Permisos)
            }).ToList();

            // Obtener estadísticas
            TotalRoles = await _context.Roles.CountAsync();
            RolesActivos = await _context.Roles.CountAsync(r => r.IsActive);
        }

        private string ObtenerResumenPermisos(string permisosJson)
        {
            try
            {
                if (string.IsNullOrEmpty(permisosJson)) return "Sin permisos configurados";

                var permisos = JsonSerializer.Deserialize<Dictionary<string, object>>(permisosJson);
                if (permisos == null || !permisos.Any()) return "Sin permisos configurados";

                var modulosConPermisos = new List<string>();
                foreach (var modulo in permisos)
                {
                    if (modulo.Value is JsonElement element && element.ValueKind == JsonValueKind.Object)
                    {
                        bool tienePermisos = false;
                        foreach (var property in element.EnumerateObject())
                        {
                            if (property.Value.ValueKind == JsonValueKind.True)
                            {
                                tienePermisos = true;
                                break;
                            }
                        }
                        if (tienePermisos)
                        {
                            modulosConPermisos.Add(char.ToUpper(modulo.Key[0]) + modulo.Key.Substring(1));
                        }
                    }
                }

                if (!modulosConPermisos.Any()) return "Sin permisos activos";

                if (modulosConPermisos.Count <= 3)
                    return string.Join(", ", modulosConPermisos);

                return $"{string.Join(", ", modulosConPermisos.Take(2))} y {modulosConPermisos.Count - 2} más";
            }
            catch
            {
                return "Permisos no válidos";
            }
        }
    }
}