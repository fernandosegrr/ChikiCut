using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Attributes;

namespace ChikiCut.web.Pages.Servicios
{
    [RequirePermission("servicios", "read")]
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;

        public DetailsModel(AppDbContext context) => _context = context;

        public Servicio Servicio { get; set; } = default!;
        public List<ServicioSucursal> SucursalesAsignadas { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var servicio = await _context.Servicios
                .Include(s => s.UsuarioCreador)
                .Include(s => s.SucursalesAsignadas.Where(ss => ss.IsActive))
                    .ThenInclude(ss => ss.Sucursal)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (servicio == null)
            {
                return NotFound();
            }

            Servicio = servicio;
            SucursalesAsignadas = servicio.SucursalesAsignadas.ToList();

            return Page();
        }
    }
}