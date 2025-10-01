using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ChikiCut.web.Services
{
    public class TemplateService : ITemplateService
    {
        private readonly AppDbContext _context;

        public TemplateService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<RoleTemplate>> GetAvailableTemplatesAsync()
        {
            return await _context.RoleTemplates
                .Where(t => t.IsActive)
                .Include(t => t.Creator)
                .OrderByDescending(t => t.IsFavorite)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<RoleTemplate>> GetFavoriteTemplatesAsync()
        {
            return await _context.RoleTemplates
                .Where(t => t.IsActive && t.IsFavorite)
                .Include(t => t.Creator)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Rol>> GetAvailableRolesAsync()
        {
            return await _context.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.Nombre)
                .ToListAsync();
        }

        public async Task<RoleTemplate?> GetTemplateAsync(long id)
        {
            return await _context.RoleTemplates
                .Include(t => t.Creator)
                .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);
        }

        public async Task<string?> GetRolePermissionsAsync(long roleId)
        {
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.Id == roleId && r.IsActive);
            
            return role?.Permisos;
        }

        public async Task<RoleTemplate> SaveAsTemplateAsync(string name, string description, string permissionsJson, long? createdBy, bool isFavorite = false)
        {
            // Verificar que no exista una plantilla con el mismo nombre
            var existingTemplate = await _context.RoleTemplates
                .FirstOrDefaultAsync(t => t.Name == name && t.IsActive);
            
            if (existingTemplate != null)
            {
                throw new InvalidOperationException($"Ya existe una plantilla con el nombre '{name}'");
            }

            var template = new RoleTemplate
            {
                Name = name,
                Description = description,
                PermissionsJson = permissionsJson,
                CreatedBy = createdBy,
                IsFavorite = isFavorite,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.RoleTemplates.Add(template);
            await _context.SaveChangesAsync();

            return template;
        }

        public async Task<bool> ToggleFavoriteAsync(long templateId)
        {
            var template = await _context.RoleTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId && t.IsActive);
            
            if (template == null)
                return false;

            template.IsFavorite = !template.IsFavorite;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteTemplateAsync(long templateId)
        {
            var template = await _context.RoleTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId);
            
            if (template == null)
                return false;

            template.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<RoleTemplate>> SearchTemplatesAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAvailableTemplatesAsync();

            return await _context.RoleTemplates
                .Where(t => t.IsActive && 
                           (t.Name.Contains(searchTerm) || 
                            (t.Description != null && t.Description.Contains(searchTerm))))
                .Include(t => t.Creator)
                .OrderByDescending(t => t.IsFavorite)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> UpdateTemplateAsync(long id, string name, string description, bool isFavorite)
        {
            var template = await _context.RoleTemplates
                .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);
            
            if (template == null)
                return false;

            // Verificar que no exista otra plantilla con el mismo nombre
            var existingTemplate = await _context.RoleTemplates
                .FirstOrDefaultAsync(t => t.Name == name && t.IsActive && t.Id != id);
            
            if (existingTemplate != null)
            {
                throw new InvalidOperationException($"Ya existe otra plantilla con el nombre '{name}'");
            }

            template.Name = name;
            template.Description = description;
            template.IsFavorite = isFavorite;

            await _context.SaveChangesAsync();
            return true;
        }

        public Dictionary<string, object> GetDefaultTemplates()
        {
            return new Dictionary<string, object>
            {
                ["admin_total"] = new Dictionary<string, object>
                {
                    ["usuarios"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = true, ["Update"] = true, ["Delete"] = true },
                    ["empleados"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = true, ["Update"] = true, ["Delete"] = true },
                    ["sucursales"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = true, ["Update"] = true, ["Delete"] = true },
                    ["puestos"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = true, ["Update"] = true, ["Delete"] = true },
                    ["proveedores"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = true, ["Update"] = true, ["Delete"] = true },
                    ["roles"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = true, ["Update"] = true, ["Delete"] = true },
                    ["reportes"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = true, ["Update"] = true, ["Delete"] = true }
                },
                ["gerente_operativo"] = new Dictionary<string, object>
                {
                    ["usuarios"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = true, ["Update"] = true, ["Delete"] = false },
                    ["empleados"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = true, ["Update"] = true, ["Delete"] = false },
                    ["sucursales"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = true, ["Update"] = true, ["Delete"] = false },
                    ["puestos"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = true, ["Update"] = true, ["Delete"] = false },
                    ["proveedores"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = true, ["Update"] = true, ["Delete"] = false },
                    ["roles"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = false, ["Update"] = false, ["Delete"] = false },
                    ["reportes"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = false, ["Update"] = false, ["Delete"] = false }
                },
                ["supervisor"] = new Dictionary<string, object>
                {
                    ["usuarios"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = false, ["Update"] = false, ["Delete"] = false },
                    ["empleados"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = false, ["Update"] = true, ["Delete"] = false },
                    ["sucursales"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = false, ["Update"] = false, ["Delete"] = false },
                    ["puestos"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = false, ["Update"] = false, ["Delete"] = false },
                    ["proveedores"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = false, ["Update"] = false, ["Delete"] = false },
                    ["roles"] = new Dictionary<string, object> { ["Read"] = false, ["Create"] = false, ["Update"] = false, ["Delete"] = false },
                    ["reportes"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = false, ["Update"] = false, ["Delete"] = false }
                },
                ["empleado_basico"] = new Dictionary<string, object>
                {
                    ["usuarios"] = new Dictionary<string, object> { ["Read"] = false, ["Create"] = false, ["Update"] = false, ["Delete"] = false },
                    ["empleados"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = false, ["Update"] = false, ["Delete"] = false },
                    ["sucursales"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = false, ["Update"] = false, ["Delete"] = false },
                    ["puestos"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = false, ["Update"] = false, ["Delete"] = false },
                    ["proveedores"] = new Dictionary<string, object> { ["Read"] = false, ["Create"] = false, ["Update"] = false, ["Delete"] = false },
                    ["roles"] = new Dictionary<string, object> { ["Read"] = false, ["Create"] = false, ["Update"] = false, ["Delete"] = false },
                    ["reportes"] = new Dictionary<string, object> { ["Read"] = false, ["Create"] = false, ["Update"] = false, ["Delete"] = false }
                },
                ["solo_consulta"] = new Dictionary<string, object>
                {
                    ["usuarios"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = false, ["Update"] = false, ["Delete"] = false },
                    ["empleados"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = false, ["Update"] = false, ["Delete"] = false },
                    ["sucursales"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = false, ["Update"] = false, ["Delete"] = false },
                    ["puestos"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = false, ["Update"] = false, ["Delete"] = false },
                    ["proveedores"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = false, ["Update"] = false, ["Delete"] = false },
                    ["roles"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = false, ["Update"] = false, ["Delete"] = false },
                    ["reportes"] = new Dictionary<string, object> { ["Read"] = true, ["Create"] = false, ["Update"] = false, ["Delete"] = false }
                }
            };
        }
    }
}