using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ChikiCut.web.Attributes
{
    public class RequireLoginAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userIdSession = context.HttpContext.Session.GetString("UserId");
            
            if (string.IsNullOrEmpty(userIdSession))
            {
                context.Result = new RedirectToPageResult("/Account/Login");
                return;
            }
        }
    }
}