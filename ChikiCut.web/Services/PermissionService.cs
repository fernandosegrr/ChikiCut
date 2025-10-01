using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ChikiCut.web.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(AppDbContext context, ILogger<PermissionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> HasPermissionAsync(long userId, string module, string action)
        {
            var user = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null || !user.PuedeAcceder)
                return false;

            return await HasRolePermissionAsync(user.RolId, module, action);
        }

        public async Task<bool> HasRolePermissionAsync(long rolId, string module, string action)
        {
            _logger.LogInformation("?? HasRolePermissionAsync: rolId={RolId}, module={Module}, action={Action}", rolId, module, action);
            
            var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Id == rolId && r.IsActive);
            
            if (rol == null)
            {
                _logger.LogWarning("? Rol no encontrado o inactivo: {RolId}", rolId);
                return false;
            }

            _logger.LogInformation("?? Rol encontrado: {RolName}, Permisos JSON: {Permisos}", rol.Nombre, rol.Permisos);

            try
            {
                if (string.IsNullOrEmpty(rol.Permisos))
                {
                    _logger.LogWarning("?? Rol sin permisos JSON: {RolName}", rol.Nombre);
                    return false;
                }

                // Parsear JSON de permisos
                using var document = JsonDocument.Parse(rol.Permisos);
                var root = document.RootElement;

                if (!root.TryGetProperty(module, out var moduleElement) || moduleElement.ValueKind != JsonValueKind.Object)
                {
                    _logger.LogWarning("? Módulo {Module} no encontrado en permisos del rol {RolName}", module, rol.Nombre);
                    return false;
                }

                // Buscar la acción específica (probando diferentes formatos de nomenclatura)
                var actionVariants = new[] { 
                    action,                                    // "read"
                    action.ToLower(),                         // "read" 
                    char.ToUpper(action[0]) + action.Substring(1).ToLower(), // "Read"
                    action.ToUpper()                          // "READ"
                };
                
                foreach (var actionVariant in actionVariants)
                {
                    if (moduleElement.TryGetProperty(actionVariant, out var actionElement))
                    {
                        _logger.LogInformation("? Acción {Action} encontrada como {ActionVariant} con valor {Value}", 
                            action, actionVariant, actionElement);
                        
                        // Manejar diferentes tipos de valores
                        switch (actionElement.ValueKind)
                        {
                            case JsonValueKind.True:
                                _logger.LogInformation("? Permiso concedido: {Module}.{Action} = true", module, action);
                                return true;
                            case JsonValueKind.False:
                                _logger.LogInformation("? Permiso denegado: {Module}.{Action} = false", module, action);
                                return false;
                            case JsonValueKind.String:
                                var stringValue = actionElement.GetString();
                                if (bool.TryParse(stringValue, out var boolValue))
                                {
                                    _logger.LogInformation("{Result} Permiso {Module}.{Action} = {Value}", 
                                        boolValue ? "?" : "?", module, action, stringValue);
                                    return boolValue;
                                }
                                break;
                        }
                    }
                }

                _logger.LogWarning("? Acción {Action} no encontrada en módulo {Module} del rol {RolName}", action, module, rol.Nombre);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "?? Error al parsear permisos del rol {RolName}: {Error}", rol.Nombre, ex.Message);
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetUserPermissionsAsync(long userId)
        {
            var user = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.Rol == null)
                return new Dictionary<string, object>();

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(user.Rol.Permisos ?? "{}") 
                       ?? new Dictionary<string, object>();
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        public Dictionary<string, object> GetDefaultPermissions()
        {
            return new Dictionary<string, object>
            {
                ["admin"] = new Dictionary<string, object>
                {
                    ["read"] = false,
                    ["access"] = false
                },
                ["sucursales"] = new Dictionary<string, object>
                {
                    ["create"] = false,
                    ["read"] = true,
                    ["update"] = false,
                    ["delete"] = false
                },
                ["empleados"] = new Dictionary<string, object>
                {
                    ["create"] = false,
                    ["read"] = true,
                    ["update"] = false,
                    ["delete"] = false,
                    ["view_salary"] = false
                },
                ["puestos"] = new Dictionary<string, object>
                {
                    ["create"] = false,
                    ["read"] = true,
                    ["update"] = false,
                    ["delete"] = false
                },
                ["proveedores"] = new Dictionary<string, object>
                {
                    ["create"] = false,
                    ["read"] = false,
                    ["update"] = false,
                    ["delete"] = false
                },
                ["servicios"] = new Dictionary<string, object>
                {
                    ["create"] = false,
                    ["read"] = true,
                    ["update"] = false,
                    ["delete"] = false,
                    ["assign"] = false
                },
                ["productos"] = new Dictionary<string, object>
                {
                    ["create"] = false,
                    ["read"] = true,
                    ["update"] = false,
                    ["delete"] = false,
                    ["assign"] = false,
                    ["manage_inventory"] = false
                },
                ["usuarios"] = new Dictionary<string, object>
                {
                    ["create"] = false,
                    ["read"] = false,
                    ["update"] = false,
                    ["delete"] = false,
                    ["manage_roles"] = false
                },
                ["roles"] = new Dictionary<string, object>
                {
                    ["create"] = false,
                    ["read"] = false,
                    ["update"] = false,
                    ["delete"] = false
                },
                ["reportes"] = new Dictionary<string, object>
                {
                    ["access"] = false,
                    ["export"] = false,
                    ["financial"] = false
                },
                ["configuracion"] = new Dictionary<string, object>
                {
                    ["access"] = false,
                    ["backup"] = false,
                    ["system_settings"] = false
                }
            };
        }

        public Dictionary<string, object> GetRoleTemplate(string roleName)
        {
            return roleName.ToLower() switch
            {
                "super administrador" => new Dictionary<string, object>
                {
                    ["admin"] = new Dictionary<string, object> { ["read"] = true, ["access"] = true },
                    ["sucursales"] = new Dictionary<string, object> { ["create"] = true, ["read"] = true, ["update"] = true, ["delete"] = true },
                    ["empleados"] = new Dictionary<string, object> { ["create"] = true, ["read"] = true, ["update"] = true, ["delete"] = true, ["view_salary"] = true },
                    ["puestos"] = new Dictionary<string, object> { ["create"] = true, ["read"] = true, ["update"] = true, ["delete"] = true },
                    ["proveedores"] = new Dictionary<string, object> { ["create"] = true, ["read"] = true, ["update"] = true, ["delete"] = true },
                    ["servicios"] = new Dictionary<string, object> { ["create"] = true, ["read"] = true, ["update"] = true, ["delete"] = true, ["assign"] = true },
                    ["productos"] = new Dictionary<string, object> { ["create"] = true, ["read"] = true, ["update"] = true, ["delete"] = true, ["assign"] = true, ["manage_inventory"] = true },
                    ["usuarios"] = new Dictionary<string, object> { ["create"] = true, ["read"] = true, ["update"] = true, ["delete"] = true, ["manage_roles"] = true },
                    ["roles"] = new Dictionary<string, object> { ["create"] = true, ["read"] = true, ["update"] = true, ["delete"] = true },
                    ["reportes"] = new Dictionary<string, object> { ["access"] = true, ["export"] = true, ["financial"] = true },
                    ["configuracion"] = new Dictionary<string, object> { ["access"] = true, ["backup"] = true, ["system_settings"] = true }
                },
                "administrador de empresa" or "administrador" => new Dictionary<string, object>
                {
                    ["admin"] = new Dictionary<string, object> { ["read"] = true, ["access"] = true },
                    ["sucursales"] = new Dictionary<string, object> { ["create"] = true, ["read"] = true, ["update"] = true, ["delete"] = false },
                    ["empleados"] = new Dictionary<string, object> { ["create"] = true, ["read"] = true, ["update"] = true, ["delete"] = false, ["view_salary"] = true },
                    ["puestos"] = new Dictionary<string, object> { ["create"] = true, ["read"] = true, ["update"] = true, ["delete"] = false },
                    ["proveedores"] = new Dictionary<string, object> { ["create"] = true, ["read"] = true, ["update"] = true, ["delete"] = false },
                    ["servicios"] = new Dictionary<string, object> { ["create"] = true, ["read"] = true, ["update"] = true, ["delete"] = false, ["assign"] = true },
                    ["productos"] = new Dictionary<string, object> { ["create"] = true, ["read"] = true, ["update"] = true, ["delete"] = false, ["assign"] = true, ["manage_inventory"] = true },
                    ["usuarios"] = new Dictionary<string, object> { ["create"] = true, ["read"] = true, ["update"] = true, ["delete"] = false, ["manage_roles"] = false },
                    ["roles"] = new Dictionary<string, object> { ["create"] = false, ["read"] = true, ["update"] = false, ["delete"] = false },
                    ["reportes"] = new Dictionary<string, object> { ["access"] = true, ["export"] = true, ["financial"] = true },
                    ["configuracion"] = new Dictionary<string, object> { ["access"] = false, ["backup"] = false, ["system_settings"] = false }
                },
                "gerente de sucursal" => new Dictionary<string, object>
                {
                    ["admin"] = new Dictionary<string, object> { ["read"] = true, ["access"] = false },
                    ["sucursales"] = new Dictionary<string, object> { ["create"] = false, ["read"] = true, ["update"] = false, ["delete"] = false },
                    ["empleados"] = new Dictionary<string, object> { ["create"] = true, ["read"] = true, ["update"] = true, ["delete"] = false, ["view_salary"] = false },
                    ["puestos"] = new Dictionary<string, object> { ["create"] = false, ["read"] = true, ["update"] = false, ["delete"] = false },
                    ["proveedores"] = new Dictionary<string, object> { ["create"] = false, ["read"] = true, ["update"] = false, ["delete"] = false },
                    ["servicios"] = new Dictionary<string, object> { ["create"] = false, ["read"] = true, ["update"] = true, ["delete"] = false, ["assign"] = true },
                    ["productos"] = new Dictionary<string, object> { ["create"] = false, ["read"] = true, ["update"] = true, ["delete"] = false, ["assign"] = true, ["manage_inventory"] = true },
                    ["usuarios"] = new Dictionary<string, object> { ["create"] = false, ["read"] = true, ["update"] = false, ["delete"] = false, ["manage_roles"] = false },
                    ["roles"] = new Dictionary<string, object> { ["create"] = false, ["read"] = true, ["update"] = false, ["delete"] = false },
                    ["reportes"] = new Dictionary<string, object> { ["access"] = true, ["export"] = false, ["financial"] = false },
                    ["configuracion"] = new Dictionary<string, object> { ["access"] = false, ["backup"] = false, ["system_settings"] = false }
                },
                "supervisor" => new Dictionary<string, object>
                {
                    ["admin"] = new Dictionary<string, object> { ["read"] = false, ["access"] = false },
                    ["sucursales"] = new Dictionary<string, object> { ["create"] = false, ["read"] = true, ["update"] = false, ["delete"] = false },
                    ["empleados"] = new Dictionary<string, object> { ["create"] = false, ["read"] = true, ["update"] = false, ["delete"] = false, ["view_salary"] = false },
                    ["puestos"] = new Dictionary<string, object> { ["create"] = false, ["read"] = true, ["update"] = false, ["delete"] = false },
                    ["proveedores"] = new Dictionary<string, object> { ["create"] = false, ["read"] = true, ["update"] = false, ["delete"] = false },
                    ["servicios"] = new Dictionary<string, object> { ["create"] = false, ["read"] = true, ["update"] = false, ["delete"] = false, ["assign"] = false },
                    ["productos"] = new Dictionary<string, object> { ["create"] = false, ["read"] = true, ["update"] = false, ["delete"] = false, ["assign"] = false, ["manage_inventory"] = false },
                    ["usuarios"] = new Dictionary<string, object> { ["create"] = false, ["read"] = false, ["update"] = false, ["delete"] = false, ["manage_roles"] = false },
                    ["roles"] = new Dictionary<string, object> { ["create"] = false, ["read"] = false, ["update"] = false, ["delete"] = false },
                    ["reportes"] = new Dictionary<string, object> { ["access"] = true, ["export"] = false, ["financial"] = false },
                    ["configuracion"] = new Dictionary<string, object> { ["access"] = false, ["backup"] = false, ["system_settings"] = false }
                },
                "empleado operativo" => new Dictionary<string, object>
                {
                    ["admin"] = new Dictionary<string, object> { ["read"] = false, ["access"] = false },
                    ["sucursales"] = new Dictionary<string, object> { ["create"] = false, ["read"] = false, ["update"] = false, ["delete"] = false },
                    ["empleados"] = new Dictionary<string, object> { ["create"] = false, ["read"] = false, ["update"] = false, ["delete"] = false, ["view_salary"] = false },
                    ["puestos"] = new Dictionary<string, object> { ["create"] = false, ["read"] = false, ["update"] = false, ["delete"] = false },
                    ["proveedores"] = new Dictionary<string, object> { ["create"] = false, ["read"] = false, ["update"] = false, ["delete"] = false },
                    ["servicios"] = new Dictionary<string, object> { ["create"] = false, ["read"] = true, ["update"] = false, ["delete"] = false, ["assign"] = false },
                    ["productos"] = new Dictionary<string, object> { ["create"] = false, ["read"] = true, ["update"] = false, ["delete"] = false, ["assign"] = false, ["manage_inventory"] = false },
                    ["usuarios"] = new Dictionary<string, object> { ["create"] = false, ["read"] = false, ["update"] = false, ["delete"] = false, ["manage_roles"] = false },
                    ["roles"] = new Dictionary<string, object> { ["create"] = false, ["read"] = false, ["update"] = false, ["delete"] = false },
                    ["reportes"] = new Dictionary<string, object> { ["access"] = false, ["export"] = false, ["financial"] = false },
                    ["configuracion"] = new Dictionary<string, object> { ["access"] = false, ["backup"] = false, ["system_settings"] = false }
                },
                "solo consulta" => new Dictionary<string, object>
                {
                    ["admin"] = new Dictionary<string, object> { ["read"] = false, ["access"] = false },
                    ["sucursales"] = new Dictionary<string, object> { ["create"] = false, ["read"] = true, ["update"] = false, ["delete"] = false },
                    ["empleados"] = new Dictionary<string, object> { ["create"] = false, ["read"] = true, ["update"] = false, ["delete"] = false, ["view_salary"] = false },
                    ["puestos"] = new Dictionary<string, object> { ["create"] = false, ["read"] = true, ["update"] = false, ["delete"] = false },
                    ["proveedores"] = new Dictionary<string, object> { ["create"] = false, ["read"] = true, ["update"] = false, ["delete"] = false },
                    ["servicios"] = new Dictionary<string, object> { ["create"] = false, ["read"] = true, ["update"] = false, ["delete"] = false, ["assign"] = false },
                    ["productos"] = new Dictionary<string, object> { ["create"] = false, ["read"] = true, ["update"] = false, ["delete"] = false, ["assign"] = false, ["manage_inventory"] = false },
                    ["usuarios"] = new Dictionary<string, object> { ["create"] = false, ["read"] = false, ["update"] = false, ["delete"] = false, ["manage_roles"] = false },
                    ["roles"] = new Dictionary<string, object> { ["create"] = false, ["read"] = false, ["update"] = false, ["delete"] = false },
                    ["reportes"] = new Dictionary<string, object> { ["access"] = true, ["export"] = false, ["financial"] = false },
                    ["configuracion"] = new Dictionary<string, object> { ["access"] = false, ["backup"] = false, ["system_settings"] = false }
                },
                _ => GetDefaultPermissions()
            };
        }

        public bool ValidatePermissionsStructure(string permissionsJson)
        {
            try
            {
                var permissions = JsonSerializer.Deserialize<Dictionary<string, object>>(permissionsJson ?? "{}");
                return permissions != null;
            }
            catch
            {
                return false;
            }
        }

        public string GetPermissionsDisplayString(string permissionsJson)
        {
            try
            {
                var permissions = JsonSerializer.Deserialize<Dictionary<string, object>>(permissionsJson ?? "{}");
                if (permissions == null) return "Sin permisos";

                var enabledModules = new List<string>();
                foreach (var module in permissions)
                {
                    var modulePerms = JsonSerializer.Deserialize<Dictionary<string, object>>(module.Value.ToString() ?? "{}");
                    if (modulePerms?.Values.Any(v => bool.Parse(v.ToString() ?? "false")) == true)
                    {
                        enabledModules.Add(module.Key);
                    }
                }

                return enabledModules.Any() ? string.Join(", ", enabledModules) : "Sin permisos";
            }
            catch
            {
                return "Configuración inválida";
            }
        }

        public List<string> GetAvailableModules()
        {
            return new List<string>
            {
                "admin", "sucursales", "empleados", "puestos", "proveedores", "servicios", "productos", "usuarios", "roles", "reportes", "configuracion"
            };
        }

        public List<string> GetAvailableActions()
        {
            return new List<string>
            {
                "create", "read", "update", "delete", "view_salary", "manage_roles", "assign", "manage_inventory", "access", "export", "financial", "backup", "system_settings"
            };
        }
    }
}