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
    public class DeleteModel : PageModel
    {
        private readonly ChikiCut.web.Data.AppDbContext _context;

        public DeleteModel(ChikiCut.web.Data.AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Sucursal Sucursal { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sucursal = await _context.Sucursals.FirstOrDefaultAsync(m => m.Id == id);

            if (sucursal == null)
            {
                return NotFound();
            }
            else
            {
                Sucursal = sucursal;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sucursal = await _context.Sucursals.FindAsync(id);
            if (sucursal != null)
            {
                Sucursal = sucursal;
                _context.Sucursals.Remove(Sucursal);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
