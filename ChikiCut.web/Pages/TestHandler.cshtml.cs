using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChikiCut.web.Pages
{
    public class TestHandlerModel : PageModel
    {
        [IgnoreAntiforgeryToken]
        public IActionResult OnPostTestPost()
        {
            return new JsonResult(new { ok = true });
        }
    }
}
