using Microsoft.AspNetCore.Mvc.RazorPages;
using ChikiCut.web.Data;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Attributes;

namespace ChikiCut.web.Pages
{
    [RequireLogin]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(AppDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public int TotalSucursales { get; set; }
        public int SucursalesActivas { get; set; }
        public int TotalEmpleados { get; set; }
        public int EmpleadosActivos { get; set; }
        public int TotalPuestos { get; set; }
        public int PuestosActivos { get; set; }

        public string UserName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public int UserLevel { get; set; } = 0;

        public async Task OnGetAsync()
        {
            UserName = HttpContext.Session.GetString("UserName") ?? "Usuario";
            UserRole = HttpContext.Session.GetString("UserRole") ?? "Sin rol";
            UserLevel = HttpContext.Session.GetInt32("UserLevel") ?? 0;

            // Obtener estadísticas básicas
            var sucursales = await _context.Sucursals.ToListAsync();
            TotalSucursales = sucursales.Count;
            SucursalesActivas = sucursales.Count(s => s.IsActive);

            // Usar Count() directamente en lugar de cargar todos los registros
            TotalEmpleados = await _context.Empleados.CountAsync();
            EmpleadosActivos = await _context.Empleados.CountAsync(e => e.IsActive);

            // Estadísticas de puestos
            TotalPuestos = await _context.Puestos.CountAsync();
            PuestosActivos = await _context.Puestos.CountAsync(p => p.IsActive);
        }
    }
}


