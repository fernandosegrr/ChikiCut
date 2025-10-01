using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Attributes;

namespace ChikiCut.web.Pages.Roles
{
    [RequirePermission("roles", "delete")]
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;

        public DeleteModel(AppDbContext context) => _context = context;

        [BindProperty]
        public Rol Rol { get; set; } = default!;

        public bool TieneUsuarios { get; set; }
        public bool EsRolPropio { get; set; }
        public bool EsUltimoAdministrador { get; set; }
        public string TipoOperacion { get; set; } = string.Empty;
        public string MensajeAdvertencia { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rol = await _context.Roles
                .Include(r => r.Usuarios.Where(u => u.IsActive))
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rol == null)
            {
                return NotFound();
            }

            Rol = rol;
            await VerificarRestriccionesAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rol = await _context.Roles
                .Include(r => r.Usuarios.Where(u => u.IsActive))
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rol == null)
            {
                return NotFound();
            }

            Rol = rol;
            await VerificarRestriccionesAsync();

            // Verificar restricciones antes de proceder
            if (EsRolPropio)
            {
                ModelState.AddModelError(string.Empty, "No puedes modificar tu propio rol.");
                return Page();
            }

            if (EsUltimoAdministrador && !rol.IsActive)
            {
                ModelState.AddModelError(string.Empty, "No se puede desactivar el último rol de administrador.");
                return Page();
            }

            if (TieneUsuarios && !rol.IsActive)
            {
                ModelState.AddModelError(string.Empty, "No se puede desactivar un rol que tiene usuarios asignados.");
                return Page();
            }

            try
            {
                // Cambiar estado del rol
                rol.IsActive = !rol.IsActive;
                rol.UpdatedAt = DateTime.UtcNow;

                _context.Update(rol);
                await _context.SaveChangesAsync();

                var accion = rol.IsActive ? "reactivado" : "desactivado";
                TempData["SuccessMessage"] = $"Rol '{rol.Nombre}' {accion} exitosamente.";

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                var accion = rol.IsActive ? "desactivar" : "reactivar";
                ModelState.AddModelError(string.Empty, $"Error al {accion} el rol: {ex.Message}");
                return Page();
            }
        }

        private async Task VerificarRestriccionesAsync()
        {
            // Verificar si tiene usuarios asignados
            TieneUsuarios = Rol.Usuarios.Any();

            // Verificar si es el rol del usuario actual
            var currentUserRole = HttpContext.Session.GetString("UserRole");
            EsRolPropio = Rol.Nombre == currentUserRole;

            // Verificar si es el último administrador
            if (Rol.NivelAcceso == 5 && Rol.IsActive)
            {
                var otrosAdmins = await _context.Roles
                    .Where(r => r.NivelAcceso == 5 && r.IsActive && r.Id != Rol.Id)
                    .CountAsync();
                EsUltimoAdministrador = otrosAdmins == 0;
            }

            // Determinar tipo de operación y mensaje
            if (Rol.IsActive)
            {
                TipoOperacion = "desactivar";
                var advertencias = new List<string>();

                if (EsRolPropio)
                    advertencias.Add("Es tu rol actual - no se puede modificar");
                if (EsUltimoAdministrador)
                    advertencias.Add("Es el último rol de administrador");
                if (TieneUsuarios)
                    advertencias.Add($"Tiene {Rol.Usuarios.Count} usuario(s) asignado(s)");

                MensajeAdvertencia = string.Join("; ", advertencias);
            }
            else
            {
                TipoOperacion = "reactivar";
                MensajeAdvertencia = "El rol estará disponible para asignar a usuarios";
            }
        }
    }
}