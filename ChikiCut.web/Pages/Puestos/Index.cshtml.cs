using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Attributes;

namespace ChikiCut.web.Pages.Puestos
{
    [RequirePermission("puestos", "read")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context) => _context = context;

        public IList<Puesto> Puestos { get; set; } = default!;
        public int TotalPuestos { get; set; }
        public int PuestosActivos { get; set; }

        public async Task OnGetAsync()
        {
            Puestos = await _context.Puestos
                .Include(p => p.Empleados.Where(e => e.IsActive))
                .OrderBy(p => p.NivelJerarquico)
                .ThenBy(p => p.Nombre)
                .ToListAsync();

            TotalPuestos = Puestos.Count;
            PuestosActivos = Puestos.Count(p => p.IsActive);
        }
    }
}