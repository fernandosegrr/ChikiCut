using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Services;
using ChikiCut.web.Helpers;
using System.Threading.Tasks;

namespace ChikiCut.web.Pages.Operaciones.Clientes
{
    public class DetailsModel : PageModel
    {
        private readonly IClienteService _clienteService;
        private readonly PermissionHelper _permissionHelper;
        public Cliente? Cliente { get; set; }
        public bool HasReadPermission { get; set; }

        public DetailsModel(IClienteService clienteService, PermissionHelper permissionHelper)
        {
            _clienteService = clienteService;
            _permissionHelper = permissionHelper;
        }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            HasReadPermission = _permissionHelper.HasPermission("clientes", "read");
            if (!HasReadPermission)
                return Forbid();
            Cliente = await _clienteService.GetByIdAsync(id);
            if (Cliente == null)
                return NotFound();
            return Page();
        }
    }
}