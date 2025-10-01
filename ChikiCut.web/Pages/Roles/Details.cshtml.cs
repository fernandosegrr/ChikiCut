using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Attributes;
using System.Text.Json;

namespace ChikiCut.web.Pages.Roles
{
    [RequirePermission("roles", "read")]
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;

        public DetailsModel(AppDbContext context) => _context = context;

        public Rol Rol { get; set; } = default!;
        public IList<Usuario> UsuariosAsignados { get; set; } = default!;
        public Dictionary<string, PermissionModule> Permisos { get; set; } = new();

        public class PermissionModule
        {
            public bool Read { get; set; }
            public bool Create { get; set; }
            public bool Update { get; set; }
            public bool Delete { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rol = await _context.Roles
                .Include(r => r.Usuarios.Where(u => u.IsActive))
                    .ThenInclude(u => u.Empleado)
                        .ThenInclude(e => e.Sucursal)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rol == null)
            {
                return NotFound();
            }

            Rol = rol;
            UsuariosAsignados = rol.Usuarios.OrderBy(u => u.Email).ToList();

            // Cargar permisos
            CargarPermisos();

            return Page();
        }

        private void CargarPermisos()
        {
            try
            {
                if (!string.IsNullOrEmpty(Rol.Permisos))
                {
                    Permisos = JsonSerializer.Deserialize<Dictionary<string, PermissionModule>>(Rol.Permisos) 
                               ?? new Dictionary<string, PermissionModule>();
                }
            }
            catch
            {
                Permisos = new Dictionary<string, PermissionModule>();
            }

            // Asegurar que todos los módulos estén presentes (INCLUYENDO servicios y productos)
            var modulos = new[] { 
                "usuarios", "empleados", "sucursales", "puestos", "proveedores", 
                "servicios", "productos", 
                "conceptosgasto", "roles", "reportes" 
            };
            foreach (var modulo in modulos)
            {
                if (!Permisos.ContainsKey(modulo))
                {
                    Permisos[modulo] = new PermissionModule();
                }
            }
        }
    }
}