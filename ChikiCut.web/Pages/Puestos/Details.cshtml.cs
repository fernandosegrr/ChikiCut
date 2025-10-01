using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;

namespace ChikiCut.web.Pages.Puestos
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;

        public DetailsModel(AppDbContext context) => _context = context;

        public Puesto? Puesto { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound("ID de puesto no proporcionado");
            }

            try
            {
                Puesto = await _context.Puestos
                    .Include(p => p.Empleados.Where(e => e.IsActive))
                    .ThenInclude(e => e.Sucursal)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (Puesto == null)
                {
                    return NotFound($"Puesto con ID {id} no encontrado");
                }

                return Page();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al cargar los detalles del puesto: {ex.Message}");
            }
        }
    }
}