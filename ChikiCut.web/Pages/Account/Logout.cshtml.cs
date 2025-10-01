using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChikiCut.web.Pages.Account
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet()
        {
            return OnPost();
        }

        public IActionResult OnPost()
        {
            // Limpiar sesión
            HttpContext.Session.Clear();
            
            // Redirigir al login
            return RedirectToPage("/Account/Login");
        }
    }
}