using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Attributes;
using ChikiCut.web.Services;
using System.Text.Json;

namespace ChikiCut.web.Pages.Roles
{
    [RequirePermission("roles", "create")]
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ITemplateService _templateService;

        public CreateModel(AppDbContext context, ITemplateService templateService)
        {
            _context = context;
            _templateService = templateService;
        }

        [BindProperty]
        public Rol Rol { get; set; } = new();

        [BindProperty]
        public Dictionary<string, PermissionModule> Permisos { get; set; } = new();

        // Nuevas propiedades para plantillas
        public List<RoleTemplate> FavoriteTemplates { get; set; } = new();
        public List<RoleTemplate> AvailableTemplates { get; set; } = new();
        public List<Rol> AvailableRoles { get; set; } = new();

        public class PermissionModule
        {
            public bool Read { get; set; }
            public bool Create { get; set; }
            public bool Update { get; set; }
            public bool Delete { get; set; }
        }

        // Handler AJAX simplificado para plantillas
        public async Task<IActionResult> OnGetAsync()
        {
            // Si es una request AJAX para plantillas
            if (Request.Query.ContainsKey("handler") && Request.Query["handler"] == "GetTemplates")
            {
                try
                {
                    // Usar plantillas reales ahora que sabemos que funciona
                    var templates = await _templateService.GetAvailableTemplatesAsync();
                    
                    // Serializar manualmente para asegurar formato correcto
                    var serializedTemplates = templates.Select(t => new {
                        id = t.Id,
                        name = t.Name ?? "",
                        description = t.Description ?? "",
                        createdBy = t.CreatedBy,
                        isFavorite = t.IsFavorite,
                        permissionCount = t.PermissionCount,
                        isSystem = t.CreatedBy == null
                    }).ToList();
                    
                    return new JsonResult(serializedTemplates);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al cargar plantillas: {ex.Message}");
                    return new JsonResult(new { error = ex.Message });
                }
            }
            
            // Comportamiento normal de la página de roles
            await LoadTemplatesAndRolesAsync();
            InicializarPermisos();
            return Page();
        }

        // Handler específico para obtener plantillas
        public async Task<IActionResult> OnGetGetTemplatesAsync()
        {
            try
            {
                Console.WriteLine("?? OnGetGetTemplatesAsync llamado");
                
                // Usar plantillas reales
                var templates = await _templateService.GetAvailableTemplatesAsync();
                
                Console.WriteLine($"? {templates.Count} plantillas encontradas");
                
                // Serializar manualmente para asegurar formato correcto
                var serializedTemplates = templates.Select(t => new {
                    id = t.Id,
                    name = t.Name ?? "",
                    description = t.Description ?? "",
                    createdBy = t.CreatedBy,
                    isFavorite = t.IsFavorite,
                    permissionCount = t.PermissionCount,
                    isSystem = t.CreatedBy == null
                }).ToList();
                
                return new JsonResult(serializedTemplates);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error en OnGetGetTemplatesAsync: {ex.Message}");
                return new JsonResult(new { error = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Remover validaciones de campos automáticos
            ModelState.Remove("Rol.CreatedAt");
            ModelState.Remove("Rol.UpdatedAt");
            ModelState.Remove("Rol.Usuarios");

            // Verificar unicidad del nombre
            if (await _context.Roles.AnyAsync(r => r.Nombre == Rol.Nombre && r.IsActive))
            {
                ModelState.AddModelError("Rol.Nombre", "Ya existe un rol activo con este nombre.");
            }

            if (!ModelState.IsValid)
            {
                await LoadTemplatesAndRolesAsync();
                return Page();
            }

            try
            {
                // Configurar campos automáticos
                Rol.CreatedAt = DateTime.UtcNow;
                Rol.IsActive = true;
                Rol.NivelAcceso = 1; // Valor por defecto, ya no se usa para lógica

                // Serializar permisos
                Rol.Permisos = JsonSerializer.Serialize(Permisos);

                _context.Roles.Add(Rol);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Rol '{Rol.Nombre}' creado exitosamente.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error al crear el rol: {ex.Message}");
                await LoadTemplatesAndRolesAsync();
                return Page();
            }
        }

        // Métodos AJAX para plantillas (iguales que en Edit)
        public async Task<IActionResult> OnGetRolePermissionsAsync(long roleId)
        {
            var permissions = await _templateService.GetRolePermissionsAsync(roleId);
            return new JsonResult(new { success = true, permissions });
        }

        public async Task<IActionResult> OnGetTemplatePermissionsAsync(long templateId)
        {
            try
            {
                var template = await _context.RoleTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId && t.IsActive);
                
                if (template == null)
                    return new JsonResult(new { success = false, message = "Plantilla no encontrada" });

                return new JsonResult(new { 
                    success = true, 
                    permissions = template.PermissionsJson, 
                    name = template.Name,
                    description = template.Description
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Error al cargar la plantilla" });
            }
        }

        public async Task<IActionResult> OnPostSaveAsTemplateAsync([FromBody] SaveTemplateRequest request)
        {
            try
            {
                // Validaciones básicas
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return new JsonResult(new { success = false, message = "El nombre es requerido" });
                }

                if (request.Name.Length < 3)
                {
                    return new JsonResult(new { success = false, message = "El nombre debe tener al menos 3 caracteres" });
                }

                // Obtener el ID del usuario actual
                var userIdStr = HttpContext.Session.GetString("UserId");
                long? userId = null;
                if (long.TryParse(userIdStr, out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                // Verificar si ya existe una plantilla con el mismo nombre para este usuario
                var existingTemplate = await _context.RoleTemplates
                    .FirstOrDefaultAsync(t => t.Name == request.Name && 
                                            t.CreatedBy == userId && 
                                            t.IsActive);

                if (existingTemplate != null)
                {
                    return new JsonResult(new { 
                        success = false, 
                        message = $"Ya tienes una plantilla llamada '{request.Name}'. Usa otro nombre." 
                    });
                }

                // Crear nueva plantilla
                var template = new RoleTemplate
                {
                    Name = request.Name,
                    Description = request.Description ?? "",
                    PermissionsJson = request.Permissions,
                    CreatedBy = userId,
                    IsFavorite = request.IsFavorite,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.RoleTemplates.Add(template);
                await _context.SaveChangesAsync();

                return new JsonResult(new { 
                    success = true, 
                    message = $"Plantilla '{request.Name}' guardada exitosamente",
                    templateId = template.Id
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { 
                    success = false, 
                    message = "Error interno del servidor. Inténtalo de nuevo.",
                    details = ex.Message
                });
            }
        }

        public async Task<IActionResult> OnPostToggleFavoriteAsync(long templateId)
        {
            try
            {
                var template = await _context.RoleTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId && t.IsActive);
                
                if (template == null)
                    return new JsonResult(new { success = false, message = "Plantilla no encontrada" });

                // Solo el creador puede modificar sus plantillas
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (long.TryParse(userIdStr, out var userId) && template.CreatedBy == userId)
                {
                    template.IsFavorite = !template.IsFavorite;
                    await _context.SaveChangesAsync();
                    return new JsonResult(new { success = true });
                }
                
                return new JsonResult(new { success = false, message = "No tienes permisos para modificar esta plantilla" });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Error al actualizar favorito" });
            }
        }

        public async Task<IActionResult> OnPostDeleteTemplateAsync(long templateId)
        {
            try
            {
                var template = await _context.RoleTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId && t.IsActive);
                
                if (template == null)
                    return new JsonResult(new { success = false, message = "Plantilla no encontrada" });

                // Solo el creador puede eliminar sus plantillas
                var userIdStr = HttpContext.Session.GetString("UserId");
                if (long.TryParse(userIdStr, out var userId) && template.CreatedBy == userId)
                {
                    template.IsActive = false; // Soft delete
                    await _context.SaveChangesAsync();
                    return new JsonResult(new { success = true });
                }
                
                return new JsonResult(new { success = false, message = "No tienes permisos para eliminar esta plantilla" });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Error al eliminar plantilla" });
            }
        }

        private async Task LoadTemplatesAndRolesAsync()
        {
            try 
            {
                FavoriteTemplates = await _templateService.GetFavoriteTemplatesAsync();
                AvailableTemplates = await _templateService.GetAvailableTemplatesAsync();
                AvailableRoles = await _templateService.GetAvailableRolesAsync();
            }
            catch (Exception ex)
            {
                // Log del error pero no fallar la página
                Console.WriteLine($"Error loading templates: {ex.Message}");
                FavoriteTemplates = new List<RoleTemplate>();
                AvailableTemplates = new List<RoleTemplate>();
                AvailableRoles = await _templateService.GetAvailableRolesAsync(); // Al menos cargar roles
            }
        }

        private void InicializarPermisos()
        {
            var modulos = new[] { 
                "admin", "operaciones", "usuarios", "empleados", "sucursales", "puestos", "proveedores", 
                "servicios", "productos", "clientes", "conceptosgasto", "roles", "reportes" 
            };
            foreach (var modulo in modulos)
            {
                Permisos[modulo] = new PermissionModule();
            }
        }

        // Clase para el request de guardar plantilla
        public class SaveTemplateRequest
        {
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public string Permissions { get; set; } = "";
            public bool IsFavorite { get; set; }
        }
    }
}