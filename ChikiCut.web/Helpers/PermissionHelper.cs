using ChikiCut.web.Services;

namespace ChikiCut.web.Helpers
{
    public class PermissionHelper
    {
        private readonly IPermissionService _permissionService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PermissionHelper(IPermissionService permissionService, IHttpContextAccessor httpContextAccessor)
        {
            _permissionService = permissionService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> HasPermissionAsync(string module, string action)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return false;

            var userIdStr = httpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
                return false;

            return await _permissionService.HasPermissionAsync(userId, module, action);
        }

        public bool HasPermission(string module, string action)
        {
            return HasPermissionAsync(module, action).GetAwaiter().GetResult();
        }

        public async Task<Dictionary<string, object>> GetUserPermissionsAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return new Dictionary<string, object>();

            var userIdStr = httpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
                return new Dictionary<string, object>();

            return await _permissionService.GetUserPermissionsAsync(userId);
        }

        public string GetUserRole()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Session.GetString("UserRole") ?? "Unknown";
        }

        public long GetUserId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return 0;

            var userIdStr = httpContext.Session.GetString("UserId");
            return long.TryParse(userIdStr, out var userId) ? userId : 0;
        }

        public string GetUserEmail()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Session.GetString("UserEmail") ?? "Unknown";
        }

        // Helper methods for common permissions - SOLO basados en permisos específicos
        public bool CanManageUsers() => HasPermission("usuarios", "create");
        public bool CanViewUsers() => HasPermission("usuarios", "read");
        public bool CanEditUsers() => HasPermission("usuarios", "update");
        public bool CanDeleteUsers() => HasPermission("usuarios", "delete");
        public bool CanManageRoles() => HasPermission("roles", "create");

        public bool CanManageEmployees() => HasPermission("empleados", "create");
        public bool CanViewEmployees() => HasPermission("empleados", "read");
        public bool CanEditEmployees() => HasPermission("empleados", "update");
        public bool CanDeleteEmployees() => HasPermission("empleados", "delete");
        public bool CanViewSalaries() => HasPermission("empleados", "view_salary");

        public bool CanManageBranches() => HasPermission("sucursales", "create");
        public bool CanViewBranches() => HasPermission("sucursales", "read");
        public bool CanEditBranches() => HasPermission("sucursales", "update");
        public bool CanDeleteBranches() => HasPermission("sucursales", "delete");

        public bool CanManagePositions() => HasPermission("puestos", "create");
        public bool CanViewPositions() => HasPermission("puestos", "read");
        public bool CanEditPositions() => HasPermission("puestos", "update");
        public bool CanDeletePositions() => HasPermission("puestos", "delete");

        public bool CanManageProviders() => HasPermission("proveedores", "create");
        public bool CanViewProviders() => HasPermission("proveedores", "read");
        public bool CanEditProviders() => HasPermission("proveedores", "update");
        public bool CanDeleteProviders() => HasPermission("proveedores", "delete");

        public bool CanViewRoles() => HasPermission("roles", "read");
        public bool CanCreateRoles() => HasPermission("roles", "create");
        public bool CanEditRoles() => HasPermission("roles", "update");
        public bool CanDeleteRoles() => HasPermission("roles", "delete");

        public bool CanAccessReports() => HasPermission("reportes", "access");
        public bool CanExportReports() => HasPermission("reportes", "export");
        public bool CanViewFinancialReports() => HasPermission("reportes", "financial");

        public bool CanAccessConfiguration() => HasPermission("configuracion", "access");
        public bool CanManageBackups() => HasPermission("configuracion", "backup");
        public bool CanManageSystemSettings() => HasPermission("configuracion", "system_settings");

        /// <summary>
        /// Verifica si el usuario puede realizar cualquier acción en un módulo específico
        /// </summary>
        public bool HasAnyPermissionInModule(string module)
        {
            return HasPermission(module, "create") ||
                   HasPermission(module, "read") ||
                   HasPermission(module, "update") ||
                   HasPermission(module, "delete");
        }

        /// <summary>
        /// Obtiene una descripción amigable del permiso requerido
        /// </summary>
        public string GetPermissionDescription(string module, string action)
        {
            return (module.ToLower(), action.ToLower()) switch
            {
                ("usuarios", "create") => "Crear nuevos usuarios",
                ("usuarios", "read") => "Ver lista de usuarios",
                ("usuarios", "update") => "Editar usuarios existentes",
                ("usuarios", "delete") => "Eliminar o desactivar usuarios",
                ("usuarios", "manage_roles") => "Gestionar roles de usuarios",
                
                ("empleados", "create") => "Registrar nuevos empleados",
                ("empleados", "read") => "Ver información de empleados",
                ("empleados", "update") => "Editar datos de empleados",
                ("empleados", "delete") => "Dar de baja empleados",
                ("empleados", "view_salary") => "Ver información salarial",
                
                ("sucursales", "create") => "Crear nuevas sucursales",
                ("sucursales", "read") => "Ver información de sucursales",
                ("sucursales", "update") => "Editar datos de sucursales",
                ("sucursales", "delete") => "Eliminar sucursales",
                
                ("puestos", "create") => "Crear nuevos puestos",
                ("puestos", "read") => "Ver catálogo de puestos",
                ("puestos", "update") => "Editar puestos existentes",
                ("puestos", "delete") => "Eliminar puestos",
                
                ("proveedores", "create") => "Registrar nuevos proveedores",
                ("proveedores", "read") => "Ver catálogo de proveedores",
                ("proveedores", "update") => "Editar datos de proveedores",
                ("proveedores", "delete") => "Eliminar proveedores",
                
                ("roles", "create") => "Crear nuevos roles",
                ("roles", "read") => "Ver configuración de roles",
                ("roles", "update") => "Editar roles existentes",
                ("roles", "delete") => "Eliminar roles",
                
                ("reportes", "access") => "Acceder a reportes",
                ("reportes", "export") => "Exportar reportes",
                ("reportes", "financial") => "Ver reportes financieros",
                
                ("configuracion", "access") => "Acceder a configuración",
                ("configuracion", "backup") => "Gestionar respaldos",
                ("configuracion", "system_settings") => "Configurar sistema",
                
                _ => $"Realizar acción '{action}' en módulo '{module}'"
            };
        }

        /// <summary>
        /// Obtiene información detallada de los permisos del usuario actual
        /// </summary>
        public async Task<UserPermissionSummary> GetUserPermissionSummaryAsync()
        {
            var permissions = await GetUserPermissionsAsync();
            var summary = new UserPermissionSummary
            {
                UserRole = GetUserRole(),
                UserEmail = GetUserEmail(),
                TotalModules = permissions.Count,
                ModulesWithAccess = new List<string>()
            };

            foreach (var module in permissions.Keys)
            {
                if (HasAnyPermissionInModule(module))
                {
                    summary.ModulesWithAccess.Add(module);
                }
            }

            summary.AccessibleModulesCount = summary.ModulesWithAccess.Count;
            return summary;
        }

        public class UserPermissionSummary
        {
            public string UserRole { get; set; } = string.Empty;
            public string UserEmail { get; set; } = string.Empty;
            public int TotalModules { get; set; }
            public int AccessibleModulesCount { get; set; }
            public List<string> ModulesWithAccess { get; set; } = new();
        }
    }
}