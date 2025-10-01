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
    [RequirePermission("roles", "update")]
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ITemplateService _templateService;

        public EditModel(AppDbContext context, ITemplateService templateService)
        {
            _context = context;
            _templateService = templateService;
        }

        [BindProperty]
        public Rol Rol { get; set; } = default!;

        [BindProperty]
        public Dictionary<string, PermissionModule> Permisos { get; set; } = new();

        public bool EsRolPropio { get; set; }
        public int UsuariosAsignados { get; set; }
        
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
        public async Task<IActionResult> OnGetAsync(long? id)
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
            
            // Comportamiento normal de la página
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
            UsuariosAsignados = rol.Usuarios.Count;

            // Verificar si es el rol del usuario actual
            var currentUserRole = HttpContext.Session.GetString("UserRole");
            EsRolPropio = rol.Nombre == currentUserRole;

            // Cargar plantillas y roles disponibles
            await LoadTemplatesAndRolesAsync();

            // Cargar permisos actuales
            CargarPermisosActuales();

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

            // Verificar si es el rol del usuario actual
            var currentUserRole = HttpContext.Session.GetString("UserRole");
            EsRolPropio = Rol.Nombre == currentUserRole;

            // Validaciones de seguridad para rol propio
            if (EsRolPropio && !Rol.IsActive)
            {
                ModelState.AddModelError("Rol.IsActive", "No puedes desactivar tu propio rol.");
            }

            // Verificar unicidad del nombre (excluyendo el rol actual)
            if (await _context.Roles.AnyAsync(r => r.Nombre == Rol.Nombre && r.Id != Rol.Id && r.IsActive))
            {
                ModelState.AddModelError("Rol.Nombre", "Ya existe otro rol activo con este nombre.");
            }

            if (!ModelState.IsValid)
            {
                await LoadTemplatesAndRolesAsync();
                CargarPermisosActuales();
                return Page();
            }

            try
            {
                var entity = await _context.Roles.FindAsync(Rol.Id);
                if (entity == null)
                {
                    return NotFound();
                }

                // Actualizar campos editables
                entity.Nombre = Rol.Nombre;
                entity.Descripcion = Rol.Descripcion;
                entity.IsActive = Rol.IsActive;
                entity.UpdatedAt = DateTime.UtcNow;
                // NivelAcceso se mantiene como estaba, ya no se modifica

                // Actualizar permisos
                entity.Permisos = JsonSerializer.Serialize(Permisos);

                _context.Update(entity);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Rol '{Rol.Nombre}' actualizado exitosamente.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error al actualizar el rol: {ex.Message}");
                await LoadTemplatesAndRolesAsync();
                CargarPermisosActuales();
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

        // Clase para el request de guardar plantilla
        public class SaveTemplateRequest
        {
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public string Permissions { get; set; } = "";
            public bool IsFavorite { get; set; }
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

        private void CargarPermisosActuales()
        {
            // Inicializar diccionario vacío
            Permisos = new Dictionary<string, PermissionModule>();

            // Asegurar que todos los módulos estén presentes primero (INCLUYENDO admin y operaciones)
            var modulos = new[] { 
                "admin", "operaciones", "usuarios", "empleados", "sucursales", "puestos", "proveedores", 
                "servicios", "productos", "clientes", "conceptosgasto", "roles", "reportes" 
            };
            foreach (var modulo in modulos)
            {
                Permisos[modulo] = new PermissionModule();
            }

            // Cargar permisos desde JSON si existe
            try
            {
                if (!string.IsNullOrEmpty(Rol.Permisos))
                {
                    using var document = JsonDocument.Parse(Rol.Permisos);
                    var root = document.RootElement;
                    foreach (var modulo in modulos)
                    {
                        if (root.TryGetProperty(modulo, out var moduloElement) && moduloElement.ValueKind == JsonValueKind.Object)
                        {
                            Permisos[modulo].Read = GetBoolProperty(moduloElement, "Read") || GetBoolProperty(moduloElement, "read");
                            Permisos[modulo].Create = GetBoolProperty(moduloElement, "Create") || GetBoolProperty(moduloElement, "create");
                            Permisos[modulo].Update = GetBoolProperty(moduloElement, "Update") || GetBoolProperty(moduloElement, "update");
                            Permisos[modulo].Delete = GetBoolProperty(moduloElement, "Delete") || GetBoolProperty(moduloElement, "delete");
                        }
                    }
                }
            }
            catch
            {
                // En caso de error, mantener la estructura vacía (todos false)
            }
        }

        private bool GetBoolProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                return property.ValueKind == JsonValueKind.True;
            }
            return false;
        }
    }
}