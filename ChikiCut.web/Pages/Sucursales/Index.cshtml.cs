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
    public class IndexModel : PageModel
    {
        private readonly ChikiCut.web.Data.AppDbContext _context;

        // Usar el constructor principal (primary constructor)
        public IndexModel(ChikiCut.web.Data.AppDbContext context) => _context = context;

        public IList<Sucursal> Sucursal { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Sucursal = await _context.Sucursals.ToListAsync();
        }
    }
}
