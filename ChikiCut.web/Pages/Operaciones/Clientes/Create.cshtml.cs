using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Services;
using ChikiCut.web.Helpers;
using System.Threading.Tasks;
using System;

namespace ChikiCut.web.Pages.Operaciones.Clientes
{
    public class CreateModel : PageModel
    {
        private readonly IClienteService _clienteService;
        private readonly PermissionHelper _permissionHelper;

        [BindProperty]
        public Cliente Cliente { get; set; } = new();

        public bool HasCreatePermission { get; set; }
        public string? DebugError { get; set; }
        public string? SuccessMessage { get; set; }

        public CreateModel(IClienteService clienteService, PermissionHelper permissionHelper)
        {
            _clienteService = clienteService;
            _permissionHelper = permissionHelper;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            HasCreatePermission = _permissionHelper.HasPermission("clientes", "create");
            if (!HasCreatePermission)
                return Forbid();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            HasCreatePermission = _permissionHelper.HasPermission("clientes", "create");
            if (!HasCreatePermission)
                return Forbid();
            if (!ModelState.IsValid)
                return Page();

            try
            {
                Cliente.CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow);
                Cliente.IsActive = true;

                if (string.IsNullOrEmpty(Cliente.NombreCompleto))
                    Cliente.NombreCompleto = "";
                if (string.IsNullOrEmpty(Cliente.Email))
                    Cliente.Email = "";

                await _clienteService.AddAsync(Cliente);
                SuccessMessage = "Cliente agregado exitosamente.";
                TempData["SuccessMessage"] = SuccessMessage;
                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                DebugError = $"Error: {ex.Message}\n" +
                             $"Inner: {ex.InnerException?.Message}\n" +
                             $"StackTrace:\n{ex.StackTrace}\n" +
                             $"Source: {ex.Source}\n" +
                             $"TargetSite: {ex.TargetSite}\n" +
                             $"ToString:\n{ex}";
                return Page();
            }
        }
    }
}