using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using ChikiCut.web.Attributes;

namespace ChikiCut.web.Pages.Operaciones
{
    [RequirePermission("operaciones", "read")]
    public class DashboardModel : PageModel
    {
        public string UserName { get; set; } = "";
        public string UserRole { get; set; } = "";
        public int UserLevel { get; set; }

        // Métricas simuladas para demostración
        public int ClientesHoy { get; set; } = 12;
        public int CitasHoy { get; set; } = 8;
        public decimal VentasHoy { get; set; } = 2450.75m;
        public int ServiciosRealizados { get; set; } = 15;

        public void OnGet()
        {
            // Obtener información del usuario desde la sesión
            UserName = HttpContext.Session.GetString("UserName") ?? "";
            UserRole = HttpContext.Session.GetString("UserRole") ?? "";
            UserLevel = HttpContext.Session.GetInt32("UserLevel") ?? 1;

            // TODO: Cargar métricas reales desde la base de datos
        }
    }
}