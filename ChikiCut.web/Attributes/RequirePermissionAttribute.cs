using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ChikiCut.web.Services;

namespace ChikiCut.web.Attributes
{
    public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _module;
        private readonly string _action;

        public RequirePermissionAttribute(string module, string action)
        {
            _module = module;
            _action = action;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // DESACTIVAR TODA LA VALIDACIÓN DE PERMISOS PARA TESTEO
            // Permitir acceso a cualquier usuario, sin importar roles ni permisos
            return;
        }

        private void LogAccessDenied(AuthorizationFilterContext context, long userId)
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequirePermissionAttribute>>();
            var userEmail = context.HttpContext.Session.GetString("UserEmail") ?? "Unknown";
            var userRole = context.HttpContext.Session.GetString("UserRole") ?? "Unknown";
            var path = context.HttpContext.Request.Path;
            var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            logger.LogWarning(
                "?? Access denied for user {UserId} ({UserEmail}, Role: {UserRole}) " +
                "attempting to access {Path} requiring {Module}:{Action} permission. IP: {IpAddress}",
                userId, userEmail, userRole, path, _module, _action, ipAddress);
        }
    }
}