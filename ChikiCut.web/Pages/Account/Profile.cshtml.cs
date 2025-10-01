using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Services;
using ChikiCut.web.Attributes;
using System.ComponentModel.DataAnnotations;

namespace ChikiCut.web.Pages.Account
{
    [RequireLogin]
    public class ProfileModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IAuthService _authService;

        public ProfileModel(AppDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        public Usuario UsuarioActual { get; set; } = default!;
        public Empleado? EmpleadoVinculado { get; set; }
        public List<AccesoReciente> AccesosRecientes { get; set; } = new();

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty]
        public PasswordChangeModel PasswordChange { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "El email es obligatorio.")]
            [EmailAddress(ErrorMessage = "Debe ser un email v�lido.")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Display(Name = "Informaci�n adicional")]
            public string? Notas { get; set; }
        }

        public class PasswordChangeModel
        {
            [Required(ErrorMessage = "La contrase�a actual es obligatoria.")]
            [DataType(DataType.Password)]
            [Display(Name = "Contrase�a Actual")]
            public string CurrentPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "La nueva contrase�a es obligatoria.")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "La contrase�a debe tener al menos 6 caracteres.")]
            [DataType(DataType.Password)]
            [Display(Name = "Nueva Contrase�a")]
            public string NewPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Debe confirmar la nueva contrase�a.")]
            [DataType(DataType.Password)]
            [Display(Name = "Confirmar Nueva Contrase�a")]
            [Compare("NewPassword", ErrorMessage = "Las contrase�as no coinciden.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public class AccesoReciente
        {
            public DateTime Fecha { get; set; }
            public string Descripcion { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var currentUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUserId) || !long.TryParse(currentUserId, out var userId))
            {
                return RedirectToPage("/Account/Login");
            }

            UsuarioActual = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Empleado)
                    .ThenInclude(e => e.Sucursal)
                .Include(u => u.Empleado)
                    .ThenInclude(e => e.PuestoNavegacion)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (UsuarioActual == null)
            {
                return RedirectToPage("/Account/Login");
            }

            EmpleadoVinculado = UsuarioActual.Empleado;

            // Cargar datos en el formulario
            Input.Email = UsuarioActual.Email;

            // Simular accesos recientes (en el futuro esto vendr�a de logs)
            CargarAccesosRecientes();

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateProfileAsync()
        {
            var currentUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUserId) || !long.TryParse(currentUserId, out var userId))
            {
                return RedirectToPage("/Account/Login");
            }

            // Remover validaciones de cambio de contrase�a para este POST
            ModelState.Remove("PasswordChange.CurrentPassword");
            ModelState.Remove("PasswordChange.NewPassword");
            ModelState.Remove("PasswordChange.ConfirmPassword");

            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            try
            {
                var usuario = await _context.Usuarios.FindAsync(userId);
                if (usuario == null)
                {
                    return RedirectToPage("/Account/Login");
                }

                // Verificar que el email no est� en uso por otro usuario
                if (!await _authService.IsEmailAvailableAsync(Input.Email, userId))
                {
                    ModelState.AddModelError("Input.Email", "Este email ya est� en uso por otro usuario.");
                    await OnGetAsync();
                    return Page();
                }

                // Actualizar datos
                usuario.Email = Input.Email;
                usuario.UpdatedAt = DateTime.UtcNow;

                // Actualizar sesi�n si cambi� el email
                if (HttpContext.Session.GetString("UserEmail") != Input.Email)
                {
                    HttpContext.Session.SetString("UserEmail", Input.Email);
                }

                _context.Update(usuario);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Perfil actualizado exitosamente.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error al actualizar el perfil: {ex.Message}");
                await OnGetAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            var currentUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUserId) || !long.TryParse(currentUserId, out var userId))
            {
                return RedirectToPage("/Account/Login");
            }

            // Remover validaciones de perfil para este POST
            ModelState.Remove("Input.Email");

            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            try
            {
                var success = await _authService.ChangePasswordAsync(userId, PasswordChange.CurrentPassword, PasswordChange.NewPassword);

                if (success)
                {
                    TempData["SuccessMessage"] = "Contrase�a cambiada exitosamente.";
                    
                    // Limpiar el formulario de contrase�a
                    PasswordChange.CurrentPassword = string.Empty;
                    PasswordChange.NewPassword = string.Empty;
                    PasswordChange.ConfirmPassword = string.Empty;
                    
                    return RedirectToPage();
                }
                else
                {
                    ModelState.AddModelError("PasswordChange.CurrentPassword", "La contrase�a actual no es correcta.");
                    await OnGetAsync();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error al cambiar la contrase�a: {ex.Message}");
                await OnGetAsync();
                return Page();
            }
        }

        private void CargarAccesosRecientes()
        {
            // Simulaci�n de accesos recientes
            // En una implementaci�n real, esto vendr�a de una tabla de logs
            AccesosRecientes = new List<AccesoReciente>
            {
                new AccesoReciente 
                { 
                    Fecha = DateTime.Now.AddMinutes(-5), 
                    Descripcion = "Acceso actual - Gesti�n de perfil" 
                },
                new AccesoReciente 
                { 
                    Fecha = UsuarioActual?.UltimoAcceso ?? DateTime.Now.AddHours(-2), 
                    Descripcion = "Sesi�n anterior - Sistema general" 
                },
                new AccesoReciente 
                { 
                    Fecha = DateTime.Now.AddDays(-1), 
                    Descripcion = "Gesti�n de usuarios" 
                },
                new AccesoReciente 
                { 
                    Fecha = DateTime.Now.AddDays(-2), 
                    Descripcion = "Consulta de reportes" 
                },
                new AccesoReciente 
                { 
                    Fecha = DateTime.Now.AddDays(-3), 
                    Descripcion = "Gesti�n de empleados" 
                }
            };
        }
    }
}