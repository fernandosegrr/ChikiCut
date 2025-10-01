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
    [RequirePermission("usuarios", "create")]
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IAuthService _authService;
        private readonly ISucursalFilterService _sucursalFilter;

        public CreateModel(AppDbContext context, IAuthService authService, ISucursalFilterService sucursalFilter)
        {
            _context = context;
            _authService = authService;
            _sucursalFilter = sucursalFilter;
        }

        public SelectList EmpleadoOptions { get; set; } = default!;
        public SelectList RolOptions { get; set; } = default!;
        public List<Sucursal> SucursalesDisponibles { get; set; } = new();
        public bool TieneAccesoGlobal { get; set; }

        [BindProperty]
        public Usuario Usuario { get; set; } = default!;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public string ConfirmPassword { get; set; } = string.Empty;

        [BindProperty]
        public List<long> SucursalesSeleccionadas { get; set; } = new();

        [BindProperty]
        public bool AsignarSucursalEmpleado { get; set; } = true;

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadEmpleadosAsync();
            await LoadRolesAsync();
            await LoadSucursalesAsync();

            // Inicializar usuario con valores predefinidos
            Usuario = new Usuario
            {
                IsActive = true
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadEmpleadosAsync();
            await LoadRolesAsync();
            await LoadSucursalesAsync();

            // Remover validaciones de campos automáticos
            ModelState.Remove("Usuario.Id");
            ModelState.Remove("Usuario.CodigoUsuario");
            ModelState.Remove("Usuario.PasswordHash");
            ModelState.Remove("Usuario.CreatedAt");
            ModelState.Remove("Usuario.UpdatedAt");
            ModelState.Remove("Usuario.Empleado");
            ModelState.Remove("Usuario.Rol");
            ModelState.Remove("Usuario.UsuarioCreador");
            ModelState.Remove("Usuario.UsuariosCreados");

            // Validaciones personalizadas
            if (string.IsNullOrEmpty(Password))
            {
                ModelState.AddModelError("Password", "La contraseña es obligatoria.");
            }
            else if (Password.Length < 6)
            {
                ModelState.AddModelError("Password", "La contraseña debe tener al menos 6 caracteres.");
            }

            if (Password != ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Las contraseñas no coinciden.");
            }

            // Verificar que el empleado no tenga ya un usuario
            var empleadoTieneUsuario = await _context.Usuarios
                .AnyAsync(u => u.EmpleadoId == Usuario.EmpleadoId);

            if (empleadoTieneUsuario)
            {
                ModelState.AddModelError("Usuario.EmpleadoId", "Este empleado ya tiene una cuenta de usuario asignada.");
            }

            // Verificar disponibilidad del email
            if (!await _authService.IsEmailAvailableAsync(Usuario.Email))
            {
                ModelState.AddModelError("Usuario.Email", "Este email ya está en uso por otro usuario.");
            }

            // VALIDAR SUCURSALES SELECCIONADAS
            if (!AsignarSucursalEmpleado && !SucursalesSeleccionadas.Any())
            {
                ModelState.AddModelError("SucursalesSeleccionadas", "Debe seleccionar al menos una sucursal o usar la sucursal del empleado.");
            }

            // Validar acceso a sucursales seleccionadas
            var userId = HttpContext.Session.GetString("UserId");
            if (long.TryParse(userId, out var currentUserId))
            {
                foreach (var sucursalId in SucursalesSeleccionadas)
                {
                    var tieneAcceso = await _sucursalFilter.UsuarioTieneAccesoSucursalAsync(currentUserId, sucursalId);
                    if (!tieneAcceso)
                    {
                        ModelState.AddModelError("SucursalesSeleccionadas", "No tienes permisos para asignar usuarios a una o más de las sucursales seleccionadas.");
                        break;
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Obtener usuario actual de la sesión para auditoría
                var currentUserIdStr = HttpContext.Session.GetString("UserId");
                if (long.TryParse(currentUserIdStr, out var createdBy))
                {
                    Usuario.CreatedBy = createdBy;
                }

                var success = await _authService.CreateUserAsync(Usuario, Password);

                if (success)
                {
                    // ASIGNAR SUCURSALES AL USUARIO RECIÉN CREADO
                    var nuevoUsuario = await _context.Usuarios
                        .Include(u => u.Empleado)
                        .FirstOrDefaultAsync(u => u.Email == Usuario.Email);

                    if (nuevoUsuario != null)
                    {
                        List<long> sucursalesParaAsignar;

                        if (AsignarSucursalEmpleado && nuevoUsuario.Empleado?.SucursalId != null)
                        {
                            // Usar la sucursal del empleado
                            sucursalesParaAsignar = [nuevoUsuario.Empleado.SucursalId];
                        }
                        else
                        {
                            // Usar sucursales seleccionadas manualmente
                            sucursalesParaAsignar = SucursalesSeleccionadas;
                        }

                        if (sucursalesParaAsignar.Any())
                        {
                            await _sucursalFilter.AsignarSucursalesUsuarioAsync(
                                nuevoUsuario.Id, 
                                sucursalesParaAsignar, 
                                createdBy
                            );
                        }
                    }

                    TempData["SuccessMessage"] = $"Usuario '{Usuario.Email}' creado exitosamente con acceso a {(AsignarSucursalEmpleado ? "su sucursal de trabajo" : $"{SucursalesSeleccionadas.Count} sucursal(es)")}.";
                    return RedirectToPage("./Index");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Error al crear el usuario. Verifique que todos los datos sean correctos.");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error al crear el usuario: {ex.Message}");
            }

            return Page();
        }

        private async Task LoadEmpleadosAsync()
        {
            // Solo empleados activos que NO tengan usuario asignado
            var empleadosSinUsuario = await _context.Empleados
                .Where(e => e.IsActive && !_context.Usuarios.Any(u => u.EmpleadoId == e.Id))
                .Include(e => e.Sucursal)
                .Include(e => e.PuestoNavegacion)
                .OrderBy(e => e.ApellidoPaterno)
                .ThenBy(e => e.Nombre)
                .Select(e => new 
                { 
                    e.Id, 
                    NombreCompleto = e.Nombre + " " + e.ApellidoPaterno + 
                                   (string.IsNullOrEmpty(e.ApellidoMaterno) ? "" : " " + e.ApellidoMaterno) +
                                   " - " + e.PuestoNavegacion.Nombre + " (" + e.Sucursal.Name + ")"
                })
                .ToListAsync();

            EmpleadoOptions = new SelectList(empleadosSinUsuario, "Id", "NombreCompleto");
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