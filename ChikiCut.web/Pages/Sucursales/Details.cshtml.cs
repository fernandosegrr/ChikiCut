using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;

namespace ChikiCut.web.Pages.Sucursales
{
    public class DetailsModel : PageModel
    {
        private readonly ChikiCut.web.Data.AppDbContext _context;

        public DetailsModel(ChikiCut.web.Data.AppDbContext context)
        {
            _context = context;
        }

        public Sucursal? Sucursal { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound("ID de sucursal no proporcionado");
            }

            try
            {
                Sucursal = await _context.Sucursals.FirstOrDefaultAsync(m => m.Id == id);
                
                if (Sucursal == null)
                {
                    return NotFound($"Sucursal con ID {id} no encontrada");
                }

                return Page();
            }
            catch (Exception ex)
            {
                // Log the exception here if you have logging configured
                return BadRequest($"Error al cargar los detalles de la sucursal: {ex.Message}");
            }
        }
    }
}
