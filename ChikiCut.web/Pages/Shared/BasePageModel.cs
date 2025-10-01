using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ChikiCut.web.Pages.Shared
{
    /// <summary>
    /// Clase base para PageModels que proporciona propiedades comunes 
    /// requeridas por el Layout (_Layout.cshtml)
    /// </summary>
    public abstract class BasePageModel : PageModel
    {
        /// <summary>
        /// Nombre del usuario actual
        /// </summary>
        public string UserName { get; set; } = "";
        
        /// <summary>
        /// Rol del usuario actual
        /// </summary>
        public string UserRole { get; set; } = "";
        
        /// <summary>
        /// Nivel de permisos del usuario (1-5)
        /// </summary>
        public int UserLevel { get; set; } = 0;

        /// <summary>
        /// Inicializa las propiedades de usuario desde la sesión
        /// </summary>
        protected virtual void InitializeUserProperties()
        {
            UserName = HttpContext.Session.GetString("UserName") ?? "";
            UserRole = HttpContext.Session.GetString("UserRole") ?? "";
            UserLevel = HttpContext.Session.GetInt32("UserLevel") ?? 0;
        }

        /// <summary>
        /// Se ejecuta automáticamente en todas las páginas para inicializar propiedades de usuario
        /// </summary>
        public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            InitializeUserProperties();
            base.OnPageHandlerExecuting(context);
        }
    }
}