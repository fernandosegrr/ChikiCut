using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Services;
using ChikiCut.web.Attributes;

namespace ChikiCut.web.Pages.Usuarios
{
    [RequirePermission("usuarios", "update")]
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IAuthService _authService;
        private readonly ISucursalFilterService _sucursalFilter;

        public EditModel(AppDbContext context, IAuthService authService, ISucursalFilterService sucursalFilter)
        {
            _context = context;
            _authService = authService;
            _sucursalFilter = sucursalFilter;
        }

        public SelectList RolOptions { get; set; } = default!;
        public List<Sucursal> SucursalesDisponibles { get; set; } = new();
        public bool TieneAccesoGlobal { get; set; }

        [BindProperty]
        public Usuario Usuario { get; set; } = default!;

        [BindProperty]
        public string? NewPassword { get; set; }

        [BindProperty]
        public string? ConfirmPassword { get; set; }

        [BindProperty]
        public bool ChangePassword { get; set; } = false;

        [BindProperty]
        public List<long> SucursalesSeleccionadas { get; set; } = new();

        [BindProperty]
        public bool UsarSucursalEmpleado { get; set; } = false;

        public string EmpleadoInfo { get; set; } = string.Empty;
        public List<string> SucursalesActuales { get; set; } = new();

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
                .Include(u => u.SucursalesAsignadas.Where(us => us.IsActive))
                    .ThenInclude(us => us.Sucursal)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
            {
                return NotFound();
            }

            Usuario = usuario;
            EmpleadoInfo = $"{usuario.Empleado.NombreCompleto} - {usuario.Empleado.PuestoNavegacion?.Nombre} ({usuario.Empleado.Sucursal?.Name})";

            // Cargar sucursales actuales del usuario
            SucursalesActuales = usuario.SucursalesAsignadas
                .Where(us => us.IsActive)
                .Select(us => us.Sucursal.Name)
                .OrderBy(name => name)
                .ToList();

            // Precargar sucursales seleccionadas
            SucursalesSeleccionadas = usuario.SucursalesAsignadas
                .Where(us => us.IsActive)
                .Select(us => us.SucursalId)
                .ToList();

            // Determinar si está usando sucursal del empleado
            if (SucursalesSeleccionadas.Count == 1 && 
                usuario.Empleado?.SucursalId != null && 
                SucursalesSeleccionadas.Contains(usuario.Empleado.SucursalId))
            {
                UsarSucursalEmpleado = true;
                SucursalesSeleccionadas.Clear(); // Limpiar para que el formulario se vea limpio
            }

            await LoadRolesAsync();
            await LoadSucursalesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadRolesAsync();
            await LoadSucursalesAsync();

            // Remover validaciones de campos automáticos
            ModelState.Remove("Usuario.CodigoUsuario");
            ModelState.Remove("Usuario.PasswordHash");
            ModelState.Remove("Usuario.CreatedAt");
            ModelState.Remove("Usuario.UpdatedAt");
            ModelState.Remove("Usuario.Empleado");
            ModelState.Remove("Usuario.Rol");
            ModelState.Remove("Usuario.UsuarioCreador");
            ModelState.Remove("Usuario.UsuariosCreados");
            ModelState.Remove("Usuario.CreatedBy");
            ModelState.Remove("EmpleadoInfo");

            // Validaciones de cambio de contraseña
            if (ChangePassword)
            {
                if (string.IsNullOrEmpty(NewPassword))
                {
                    ModelState.AddModelError("NewPassword", "La nueva contraseña es obligatoria.");
                }
                else if (NewPassword.Length < 6)
                {
                    ModelState.AddModelError("NewPassword", "La contraseña debe tener al menos 6 caracteres.");
                }

                if (NewPassword != ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "Las contraseñas no coinciden.");
                }
            }

            // Verificar disponibilidad del email (excluyendo el usuario actual)
            if (!await _authService.IsEmailAvailableAsync(Usuario.Email, Usuario.Id))
            {
                ModelState.AddModelError("Usuario.Email", "Este email ya está en uso por otro usuario.");
            }

            // VALIDAR SUCURSALES SELECCIONADAS
            if (!UsarSucursalEmpleado && !SucursalesSeleccionadas.Any())
            {
                ModelState.AddModelError("SucursalesSeleccionadas", "Debe seleccionar al menos una sucursal o usar la sucursal del empleado.");
            }

            // Validar acceso a sucursales seleccionadas
            var currentUserId = HttpContext.Session.GetString("UserId");
            if (long.TryParse(currentUserId, out var currentUserIdLong))
            {
                foreach (var sucursalId in SucursalesSeleccionadas)
                {
                    var tieneAcceso = await _sucursalFilter.UsuarioTieneAccesoSucursalAsync(currentUserIdLong, sucursalId);
                    if (!tieneAcceso)
                    {
                        ModelState.AddModelError("SucursalesSeleccionadas", "No tienes permisos para asignar usuarios a una o más de las sucursales seleccionadas.");
                        break;
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                // Recargar información del empleado para mostrar en caso de error
                var empleadoInfo = await _context.Empleados
                    .Include(e => e.Sucursal)
                    .Include(e => e.PuestoNavegacion)
                    .Where(e => e.Id == Usuario.EmpleadoId)
                    .FirstOrDefaultAsync();

                if (empleadoInfo != null)
                {
                    EmpleadoInfo = $"{empleadoInfo.NombreCompleto} - {empleadoInfo.PuestoNavegacion?.Nombre} ({empleadoInfo.Sucursal?.Name})";
                }

                return Page();
            }

            try
            {
                var entity = await _context.Usuarios
                    .Include(u => u.Empleado)
                    .FirstOrDefaultAsync(u => u.Id == Usuario.Id);
                
                if (entity == null)
                {
                    return NotFound();
                }

                // Actualizar campos editables
                entity.Email = Usuario.Email;
                entity.RolId = Usuario.RolId;
                entity.IsActive = Usuario.IsActive;
                entity.UpdatedAt = DateTime.UtcNow;

                // Cambiar contraseña si se solicitó
                if (ChangePassword && !string.IsNullOrEmpty(NewPassword))
                {
                    await _authService.ResetPasswordAsync(Usuario.Id, NewPassword);
                }

                _context.Update(entity);
                await _context.SaveChangesAsync();

                // ACTUALIZAR SUCURSALES ASIGNADAS
                List<long> sucursalesParaAsignar;

                if (UsarSucursalEmpleado && entity.Empleado?.SucursalId != null)
                {
                    // Usar la sucursal del empleado
                    sucursalesParaAsignar = [entity.Empleado.SucursalId];
                }
                else
                {
                    // Usar sucursales seleccionadas manualmente
                    sucursalesParaAsignar = SucursalesSeleccionadas;
                }

                if (sucursalesParaAsignar.Any())
                {
                    await _sucursalFilter.AsignarSucursalesUsuarioAsync(
                        Usuario.Id, 
                        sucursalesParaAsignar, 
                        currentUserIdLong
                    );
                }

                TempData["SuccessMessage"] = $"Usuario '{Usuario.Email}' actualizado exitosamente con acceso a {(UsarSucursalEmpleado ? "su sucursal de trabajo" : $"{SucursalesSeleccionadas.Count} sucursal(es)")}.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error al actualizar el usuario: {ex.Message}");
                return Page();
            }
        }

        private async Task LoadRolesAsync()
        {
            var roles = await _context.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.NivelAcceso)
                .ThenBy(r => r.Nombre)
                .Select(r => new { r.Id, NombreCompleto = $"{r.Nombre} - {r.NivelDescripcion}" })
                .ToListAsync();

            RolOptions = new SelectList(roles, "Id", "NombreCompleto");
        }

        private async Task LoadSucursalesAsync()
        {
            // Obtener ID del usuario actual
            var userId = HttpContext.Session.GetString("UserId");
            if (!long.TryParse(userId, out var usuarioId))
            {
                SucursalesDisponibles = new List<Sucursal>();
                return;
            }

            // Verificar si tiene acceso global
            TieneAccesoGlobal = await _sucursalFilter.TieneAccesoGlobalAsync(usuarioId);

            if (TieneAccesoGlobal)
            {
                // Si tiene acceso global, mostrar todas las sucursales
                SucursalesDisponibles = await _context.Sucursals
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Name)
                    .ToListAsync();
            }
            else
            {
                // Solo mostrar sucursales a las que tiene acceso
                var sucursalesIds = await _sucursalFilter.GetSucursalesUsuarioAsync(usuarioId);
                SucursalesDisponibles = await _context.Sucursals
                    .Where(s => s.IsActive && sucursalesIds.Contains(s.Id))
                    .OrderBy(s => s.Name)
                    .ToListAsync();
            }
        }
    }
}