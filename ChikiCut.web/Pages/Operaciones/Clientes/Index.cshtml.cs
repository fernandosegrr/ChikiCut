using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Services;
using ChikiCut.web.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace ChikiCut.web.Pages.Operaciones.Clientes
{
    public class IndexModel : PageModel
    {
        private readonly IClienteService _clienteService;
        private readonly PermissionHelper _permissionHelper;
        public List<Cliente> Clientes { get; set; } = new();
        public bool HasReadPermission { get; set; }
        public bool HasUpdatePermission { get; set; }
        public bool HasDeletePermission { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? Estado { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? Registrados { get; set; }

        public IndexModel(IClienteService clienteService, PermissionHelper permissionHelper)
        {
            _clienteService = clienteService;
            _permissionHelper = permissionHelper;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            HasReadPermission = _permissionHelper.HasPermission("clientes", "read");
            HasUpdatePermission = _permissionHelper.HasPermission("clientes", "update");
            HasDeletePermission = _permissionHelper.HasPermission("clientes", "delete");
            if (!HasReadPermission)
                return Forbid();
            var clientes = await _clienteService.GetAllAsync();

            // Filtro de búsqueda
            if (!string.IsNullOrWhiteSpace(Search))
            {
                clientes = clientes.Where(c =>
                    (!string.IsNullOrEmpty(c.NombreCompleto) && c.NombreCompleto.Contains(Search, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(c.Email) && c.Email.Contains(Search, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(c.Telefono) && c.Telefono.Contains(Search, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            // Filtro de estado
            if (!string.IsNullOrWhiteSpace(Estado))
            {
                if (Estado == "Activos")
                    clientes = clientes.Where(c => c.IsActive).ToList();
                else if (Estado == "Inactivos")
                    clientes = clientes.Where(c => !c.IsActive).ToList();
                else if (Estado == "VIP")
                    clientes = clientes.Where(c => c.Notas != null && c.Notas.Contains("VIP", StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Filtro de fecha de registro
            if (!string.IsNullOrWhiteSpace(Registrados))
            {
                var now = DateTime.UtcNow;
                if (Registrados == "Semana")
                    clientes = clientes.Where(c => c.CreatedAt >= DateOnly.FromDateTime(now.AddDays(-7))).ToList();
                else if (Registrados == "Mes")
                    clientes = clientes.Where(c => c.CreatedAt >= DateOnly.FromDateTime(now.AddMonths(-1))).ToList();
                else if (Registrados == "Año")
                    clientes = clientes.Where(c => c.CreatedAt >= DateOnly.FromDateTime(now.AddYears(-1))).ToList();
            }

            Clientes = clientes;
            return Page();
        }

        public async Task<IActionResult> OnPostActivarAsync(long id)
        {
            HasUpdatePermission = _permissionHelper.HasPermission("clientes", "update");
            if (!HasUpdatePermission)
                return Forbid();
            var cliente = await _clienteService.GetByIdAsync(id);
            if (cliente == null)
                return NotFound();
            cliente.IsActive = true;
            cliente.UpdatedAt = DateOnly.FromDateTime(DateTime.UtcNow);
            await _clienteService.UpdateAsync(cliente);
            TempData["SuccessMessage"] = "Cliente activado exitosamente.";
            return RedirectToPage();
        }
    }
}