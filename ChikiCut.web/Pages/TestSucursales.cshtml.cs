using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ChikiCut.web.Services;
using ChikiCut.web.Data;
using Microsoft.EntityFrameworkCore;

namespace ChikiCut.web.Pages
{
    public class TestSucursalesModel : PageModel
    {
        private readonly ISucursalFilterService _sucursalFilter;
        private readonly AppDbContext _context;

        public TestSucursalesModel(ISucursalFilterService sucursalFilter, AppDbContext context)
        {
            _sucursalFilter = sucursalFilter;
            _context = context;
        }

        public List<TestResult> Resultados { get; set; } = new();
        public List<UserSucursalInfo> UsuariosInfo { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Obtener todos los usuarios con sus asignaciones
            UsuariosInfo = await _context.Usuarios
                .Include(u => u.Empleado)
                    .ThenInclude(e => e.Sucursal)
                .Include(u => u.SucursalesAsignadas.Where(us => us.IsActive))
                    .ThenInclude(us => us.Sucursal)
                .Where(u => u.IsActive)
                .Select(u => new UserSucursalInfo
                {
                    UsuarioId = u.Id,
                    CodigoUsuario = u.CodigoUsuario,
                    Email = u.Email,
                    SucursalEmpleado = u.Empleado.Sucursal.Name,
                    SucursalesAsignadas = u.SucursalesAsignadas
                        .Where(us => us.IsActive)
                        .Select(us => us.Sucursal.Name)
                        .ToList()
                })
                .ToListAsync();

            // Probar el servicio con algunos usuarios
            foreach (var user in UsuariosInfo.Take(5))
            {
                var sucursales = await _sucursalFilter.GetSucursalesUsuarioAsync(user.UsuarioId);
                var nombrestSucursales = await _context.Sucursals
                    .Where(s => sucursales.Contains(s.Id))
                    .Select(s => s.Name)
                    .ToListAsync();

                Resultados.Add(new TestResult
                {
                    Usuario = user.CodigoUsuario,
                    SucursalesIds = sucursales,
                    SucursalesNombres = nombrestSucursales
                });
            }

            return Page();
        }

        public class UserSucursalInfo
        {
            public long UsuarioId { get; set; }
            public string CodigoUsuario { get; set; } = "";
            public string Email { get; set; } = "";
            public string SucursalEmpleado { get; set; } = "";
            public List<string> SucursalesAsignadas { get; set; } = new();
        }

        public class TestResult
        {
            public string Usuario { get; set; } = "";
            public List<long> SucursalesIds { get; set; } = new();
            public List<string> SucursalesNombres { get; set; } = new();
        }
    }
}