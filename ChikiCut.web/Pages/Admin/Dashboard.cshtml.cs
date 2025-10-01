using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using ChikiCut.web.Attributes;
using ChikiCut.web.Pages.Shared;

namespace ChikiCut.web.Pages.Admin
{
    // RESTAURAR la validación de permisos correcta
    [RequirePermission("admin", "read")]
    public class DashboardModel : BasePageModel
    {
        public void OnGet()
        {
            // Las propiedades de usuario se inicializan automáticamente en BasePageModel
            
            // Verificar que tenga permisos de administración por nivel (backup)
            if (UserLevel < 3)
            {
                Response.Redirect("/Account/AccessDenied?module=admin&action=read");
                return;
            }
        }
    }
}