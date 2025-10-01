using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Attributes;

namespace ChikiCut.web.Pages.Usuarios
{
    [RequirePermission("usuarios", "delete")]
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;

        public DeleteModel(AppDbContext context) => _context = context;

        public Usuario Usuario { get; set; } = default!;
        public string TipoOperacion { get; set; } = string.Empty;
        public bool TieneRelaciones { get; set; } = false;
        public string MensajeRelaciones { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios
                .Include(u => u.Empleado)
                    .ThenInclude(e => e.Sucursal)
                .Include(u => u.Empleado)
                    .ThenInclude(e => e.PuestoNavegacion)
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
            {
                return NotFound();
            }

            Usuario = usuario;
            TipoOperacion = usuario.IsActive ? "desactivar" : "reactivar";

            // Verificar si es seguro desactivar el usuario
            if (usuario.IsActive)
            {
                await VerificarRelacionesAsync(usuario);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios
                .Include(u => u.Empleado)
                    .ThenInclude(e => e.Sucursal)
                .Include(u => u.Empleado)
                    .ThenInclude(e => e.PuestoNavegacion)
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
            {
                return NotFound();
            }

            // Verificaciones antes de desactivar
            if (usuario.IsActive)
            {
                await VerificarRelacionesAsync(usuario);
                
                if (TieneRelaciones)
                {
                    Usuario = usuario;
                    TipoOperacion = "desactivar";
                    return Page();
                }
            }

            try
            {
                // Cambiar estado del usuario
                usuario.IsActive = !usuario.IsActive;
                usuario.UpdatedAt = DateTime.UtcNow;

                // Si se está desactivando, limpiar intentos fallidos y bloqueos
                if (!usuario.IsActive)
                {
                    usuario.IntentosFallidos = 0;
                    usuario.BloqueadoHasta = null;
                }

                _context.Update(usuario);
                await _context.SaveChangesAsync();

                // Mensaje de confirmación según la acción
                TempData["SuccessMessage"] = usuario.IsActive ?
                    $"Usuario '{usuario.Email}' reactivado exitosamente." :
                    $"Usuario '{usuario.Email}' desactivado exitosamente.";

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                var accion = usuario.IsActive ? "reactivar" : "desactivar";
                ModelState.AddModelError(string.Empty, $"Error al {accion} el usuario: {ex.Message}");
                Usuario = usuario;
                TipoOperacion = usuario.IsActive ? "desactivar" : "reactivar";
                return Page();
            }
        }

        private async Task VerificarRelacionesAsync(Usuario usuario)
        {
            var warnings = new List<string>();

            // Verificar si es el único administrador activo
            if (usuario.Rol.NivelAcceso == 5) // Administrador
            {
                var adminCount = await _context.Usuarios
                    .Include(u => u.Rol)
                    .Where(u => u.IsActive && u.Rol.NivelAcceso == 5 && u.Id != usuario.Id)
                    .CountAsync();

                if (adminCount == 0)
                {
                    warnings.Add("Es el único administrador activo en el sistema");
                }
            }

            // Verificar si es el único gerente de su sucursal
            if (usuario.Rol.NivelAcceso == 4 && usuario.Empleado?.SucursalId != null) // Gerente
            {
                var gerenteCount = await _context.Usuarios
                    .Include(u => u.Rol)
                    .Include(u => u.Empleado)
                    .Where(u => u.IsActive && 
                           u.Rol.NivelAcceso >= 4 && 
                           u.Empleado.SucursalId == usuario.Empleado.SucursalId && 
                           u.Id != usuario.Id)
                    .CountAsync();

                if (gerenteCount == 0)
                {
                    warnings.Add($"Es el único usuario con nivel gerencial en {usuario.Empleado.Sucursal?.Name}");
                }
            }

            // Verificar si ha creado otros usuarios
            var usuariosCreados = await _context.Usuarios
                .Where(u => u.CreatedBy == usuario.Id && u.IsActive)
                .CountAsync();

            if (usuariosCreados > 0)
            {
                warnings.Add($"Ha creado {usuariosCreados} usuario(s) en el sistema");
            }

            TieneRelaciones = warnings.Any();
            MensajeRelaciones = string.Join("; ", warnings);
        }
    }
}