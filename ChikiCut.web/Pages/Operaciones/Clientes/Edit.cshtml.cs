using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Services;
using ChikiCut.web.Helpers;
using System.Threading.Tasks;
using System;

namespace ChikiCut.web.Pages.Operaciones.Clientes
{
    public class EditModel : PageModel
    {
        private readonly IClienteService _clienteService;
        private readonly PermissionHelper _permissionHelper;
        [BindProperty]
        public Cliente Cliente { get; set; } = new();
        public bool HasUpdatePermission { get; set; }
        public string? SuccessMessage { get; set; }

        public EditModel(IClienteService clienteService, PermissionHelper permissionHelper)
        {
            _clienteService = clienteService;
            _permissionHelper = permissionHelper;
        }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            HasUpdatePermission = _permissionHelper.HasPermission("clientes", "update");
            if (!HasUpdatePermission)
                return Forbid();
            var cliente = await _clienteService.GetByIdAsync(id);
            if (cliente == null)
                return NotFound();
            Cliente = cliente;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            HasUpdatePermission = _permissionHelper.HasPermission("clientes", "update");
            if (!HasUpdatePermission)
                return Forbid();
            if (!ModelState.IsValid)
                return Page();
            Cliente.UpdatedAt = DateOnly.FromDateTime(DateTime.UtcNow);
            await _clienteService.UpdateAsync(Cliente);
            SuccessMessage = "Cliente editado exitosamente.";
            TempData["SuccessMessage"] = SuccessMessage;
            return RedirectToPage("Index");
        }

        public async Task<IActionResult> OnPostActivarAsync()
        {
            HasUpdatePermission = _permissionHelper.HasPermission("clientes", "update");
            if (!HasUpdatePermission)
                return Forbid();
            var cliente = await _clienteService.GetByIdAsync(Cliente.Id);
            if (cliente == null)
                return NotFound();
            cliente.IsActive = true;
            cliente.UpdatedAt = DateOnly.FromDateTime(DateTime.UtcNow);
            await _clienteService.UpdateAsync(cliente);
            TempData["SuccessMessage"] = "Cliente activado exitosamente.";
            return RedirectToPage("Index");
        }
    }
}