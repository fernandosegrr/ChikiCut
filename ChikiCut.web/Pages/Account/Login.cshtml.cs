using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ChikiCut.web.Services;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace ChikiCut.web.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(IAuthService authService, ILogger<LoginModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }
        public string? ExceptionMessage { get; set; } // Para mostrar el error

        public class InputModel
        {
            [Required(ErrorMessage = "El email es obligatorio.")]
            [EmailAddress(ErrorMessage = "Debe ser un email válido.")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "La contraseña es obligatoria.")]
            [DataType(DataType.Password)]
            [Display(Name = "Contraseña")]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Recordarme")]
            public bool RememberMe { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
        {
            // Si ya está logueado, redirigir al home
            if (HttpContext.Session.GetString("UserId") != null)
            {
                return RedirectToPage("/Index");
            }

            ReturnUrl = returnUrl ?? Url.Content("~/");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");

            if (ModelState.IsValid)
            {
                try
                {
                    _logger.LogInformation("Intento de login para email: {Email}", Input.Email);
                    
                    var isValid = await _authService.ValidateUserAsync(Input.Email, Input.Password);

                    if (isValid)
                    {
                        var user = await _authService.GetUserByEmailAsync(Input.Email);
                        
                        if (user != null)
                        {
                            _logger.LogInformation("Login exitoso para: {Email}", Input.Email);
                            
                            // Crear sesión (legacy, puedes mantenerla si la usas en otras partes)
                            HttpContext.Session.SetString("UserId", user.Id.ToString());
                            HttpContext.Session.SetString("UserEmail", user.Email);
                            HttpContext.Session.SetString("UserName", user.NombreCompleto);
                            HttpContext.Session.SetString("UserRole", user.Rol.Nombre);
                            HttpContext.Session.SetInt32("UserLevel", user.Rol.NivelAcceso);
                            HttpContext.Session.SetString("UserPermissions", user.Rol.Permisos);
                            HttpContext.Session.SetString("UserCode", user.CodigoUsuario);

                            // Crear cookie de autenticación
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                                new Claim(ClaimTypes.Name, user.NombreCompleto),
                                new Claim(ClaimTypes.Email, user.Email),
                                new Claim("userId", user.Id.ToString()),
                                new Claim("rol", user.Rol.Nombre)
                            };
                            var claimsIdentity = new ClaimsIdentity(claims, "ChikiCutCookie");
                            var authProperties = new AuthenticationProperties
                            {
                                IsPersistent = Input.RememberMe,
                                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                            };
                            await HttpContext.SignInAsync("ChikiCutCookie", new ClaimsPrincipal(claimsIdentity), authProperties);

                            // Actualizar último acceso
                            await _authService.UpdateLastAccessAsync(user.Id);

                            return LocalRedirect(ReturnUrl);
                        }
                    }

                    _logger.LogWarning("Intento de login fallido para: {Email}", Input.Email);
                    ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error durante el login para email: {Email}", Input.Email);
                    ExceptionMessage = ex.Message + (ex.InnerException != null ? " | " + ex.InnerException.Message : "");
                    ModelState.AddModelError(string.Empty, "Error al intentar iniciar sesión. Intente nuevamente.\n" + ExceptionMessage);
                }
            }

            return Page();
        }
    }
}