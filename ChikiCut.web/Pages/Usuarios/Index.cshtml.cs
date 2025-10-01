using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Attributes;

namespace ChikiCut.web.Pages.Usuarios
{
    [RequirePermission("usuarios", "read")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context) => _context = context;

        public IList<Usuario> Usuarios { get; set; } = default!;
        public int TotalUsuarios { get; set; }
        public int UsuariosActivos { get; set; }
        public int UsuariosBloqueados { get; set; }

        public async Task OnGetAsync()
        {
            Usuarios = await _context.Usuarios
                .Include(u => u.Empleado)
                    .ThenInclude(e => e.Sucursal)
                .Include(u => u.Empleado)
                    .ThenInclude(e => e.PuestoNavegacion)
                .Include(u => u.Rol)
                .OrderBy(u => u.Empleado.ApellidoPaterno)
                .ThenBy(u => u.Empleado.Nombre)
                .ToListAsync();

            TotalUsuarios = Usuarios.Count;
            UsuariosActivos = Usuarios.Count(u => u.IsActive && !u.EstaBloqueado);
            UsuariosBloqueados = Usuarios.Count(u => u.EstaBloqueado);
        }
    }
}