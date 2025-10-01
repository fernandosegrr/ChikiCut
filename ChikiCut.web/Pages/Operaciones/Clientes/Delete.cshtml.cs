using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Services;
using System.Threading.Tasks;

namespace ChikiCut.web.Pages.Operaciones.Clientes
{
    public class DeleteModel : PageModel
    {
        private readonly IClienteService _clienteService;
        public Cliente Cliente { get; set; } = new();
        [BindProperty]
        public string ConfirmDelete { get; set; } = string.Empty;
        public string? SuccessMessage { get; set; }

        public DeleteModel(IClienteService clienteService)
        {
            _clienteService = clienteService;
        }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            var cliente = await _clienteService.GetByIdAsync(id);
            if (cliente == null)
                return NotFound();
            Cliente = cliente;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long id)
        {
            var cliente = await _clienteService.GetByIdAsync(id);
            if (cliente == null)
                return NotFound();
            if (ConfirmDelete != "ELIMINAR")
            {
                ModelState.AddModelError("ConfirmDelete", "Debes escribir 'ELIMINAR' para confirmar.");
                Cliente = cliente;
                return Page();
            }
            await _clienteService.DeleteAsync(id);
            SuccessMessage = "Cliente eliminado exitosamente.";
            TempData["SuccessMessage"] = SuccessMessage;
            return RedirectToPage("Index");
        }
    }
}