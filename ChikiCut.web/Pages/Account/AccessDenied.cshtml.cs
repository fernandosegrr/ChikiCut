using Microsoft.AspNetCore.Mvc.RazorPages;
using ChikiCut.web.Attributes;
using ChikiCut.web.Helpers;
using ChikiCut.web.Pages.Shared;

namespace ChikiCut.web.Pages.Account
{
    [RequireLogin]
    public class AccessDeniedModel : BasePageModel
    {
        private readonly PermissionHelper _permissionHelper;

        public AccessDeniedModel(PermissionHelper permissionHelper)
        {
            _permissionHelper = permissionHelper;
        }

        // Propiedades específicas de AccessDenied
        public string? Module { get; set; }
        public string? Action { get; set; }
        public string? UserEmail { get; set; }
        public string? RequiredPermission { get; set; }
        public string? ReturnUrl { get; set; }
        public bool HasRelatedPermissions { get; set; }
        public List<string> AlternativeActions { get; set; } = new();
        public PermissionHelper.UserPermissionSummary? PermissionSummary { get; set; }

        public async Task OnGetAsync(string? module, string? action, string? returnUrl)
        {
            // Las propiedades de usuario ya se inicializan automáticamente en BasePageModel
            
            Module = module ?? "desconocido";
            Action = action ?? "desconocida";
            ReturnUrl = returnUrl;

            // Obtener información del usuario actual
            UserEmail = _permissionHelper.GetUserEmail();
            RequiredPermission = _permissionHelper.GetPermissionDescription(Module, Action);

            // Obtener resumen de permisos del usuario
            PermissionSummary = await _permissionHelper.GetUserPermissionSummaryAsync();

            // Verificar permisos relacionados
            HasRelatedPermissions = _permissionHelper.HasAnyPermissionInModule(Module);
            
            // Sugerir acciones alternativas
            await GenerateAlternativeActionsAsync();
        }

        private async Task GenerateAlternativeActionsAsync()
        {
            var alternatives = new List<string>();

            if (Module?.ToLower() == "usuarios")
            {
                if (await _permissionHelper.HasPermissionAsync("usuarios", "read")) 
                    alternatives.Add("Ver lista de usuarios");
                if (await _permissionHelper.HasPermissionAsync("usuarios", "update")) 
                    alternatives.Add("Editar usuarios existentes");
                if (await _permissionHelper.HasPermissionAsync("usuarios", "create")) 
                    alternatives.Add("Crear nuevos usuarios");
            }
            else if (Module?.ToLower() == "empleados")
            {
                if (await _permissionHelper.HasPermissionAsync("empleados", "read")) 
                    alternatives.Add("Ver información de empleados");
                if (await _permissionHelper.HasPermissionAsync("empleados", "update")) 
                    alternatives.Add("Editar datos de empleados");
                if (await _permissionHelper.HasPermissionAsync("empleados", "create")) 
                    alternatives.Add("Crear nuevos empleados");
            }
            else if (Module?.ToLower() == "sucursales")
            {
                if (await _permissionHelper.HasPermissionAsync("sucursales", "read")) 
                    alternatives.Add("Ver información de sucursales");
                if (await _permissionHelper.HasPermissionAsync("sucursales", "update")) 
                    alternatives.Add("Editar datos de sucursales");
                if (await _permissionHelper.HasPermissionAsync("sucursales", "create")) 
                    alternatives.Add("Crear nuevas sucursales");
            }
            else if (Module?.ToLower() == "puestos")
            {
                if (await _permissionHelper.HasPermissionAsync("puestos", "read")) 
                    alternatives.Add("Ver catálogo de puestos");
                if (await _permissionHelper.HasPermissionAsync("puestos", "update")) 
                    alternatives.Add("Editar puestos existentes");
                if (await _permissionHelper.HasPermissionAsync("puestos", "create")) 
                    alternatives.Add("Crear nuevos puestos");
            }
            else if (Module?.ToLower() == "proveedores")
            {
                if (await _permissionHelper.HasPermissionAsync("proveedores", "read")) 
                    alternatives.Add("Ver catálogo de proveedores");
                if (await _permissionHelper.HasPermissionAsync("proveedores", "update")) 
                    alternatives.Add("Editar datos de proveedores");
                if (await _permissionHelper.HasPermissionAsync("proveedores", "create")) 
                    alternatives.Add("Crear nuevos proveedores");
            }
            else if (Module?.ToLower() == "roles")
            {
                if (await _permissionHelper.HasPermissionAsync("roles", "read")) 
                    alternatives.Add("Ver configuración de roles");
                if (await _permissionHelper.HasPermissionAsync("roles", "update")) 
                    alternatives.Add("Editar roles existentes");
            }
            else if (Module?.ToLower() == "reportes")
            {
                if (await _permissionHelper.HasPermissionAsync("reportes", "access")) 
                    alternatives.Add("Acceder a reportes básicos");
                if (await _permissionHelper.HasPermissionAsync("reportes", "export")) 
                    alternatives.Add("Exportar reportes");
            }

            AlternativeActions = alternatives;
        }
    }
}