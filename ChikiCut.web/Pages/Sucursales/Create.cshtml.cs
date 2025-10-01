using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;

namespace ChikiCut.web.Pages.Sucursales
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        // Usar el constructor principal simplificado
        public CreateModel(AppDbContext context) => _context = context;

        public IActionResult OnGet()
        {
            // Horarios predefinidos: Lunes a Viernes 9:00-17:00, Sábado 9:00-13:00, Domingo cerrado
            var predefinedHours = new {
                mon = new[] { new { open = "09:00", close = "17:00" } },
                tue = new[] { new { open = "09:00", close = "17:00" } },
                wed = new[] { new { open = "09:00", close = "17:00" } },
                thu = new[] { new { open = "09:00", close = "17:00" } },
                fri = new[] { new { open = "09:00", close = "17:00" } },
                sat = new[] { new { open = "09:00", close = "13:00" } },
                sun = new object[0] // Array vacío para domingo (cerrado)
            };

            // Inicializar valores por defecto
            Sucursal = new Sucursal
            {
                Country = "MX",
                IsActive = true,
                OpeningHours = System.Text.Json.JsonSerializer.Serialize(predefinedHours)
            };
            return Page();
        }

        [BindProperty]
        public Sucursal Sucursal { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            // Remover validaciones de campos que se manejan automáticamente
            ModelState.Remove("Sucursal.Id");
            ModelState.Remove("Sucursal.CreatedAt");
            ModelState.Remove("Sucursal.UpdatedAt");

            // Configurar valores por defecto antes de validar
            if (string.IsNullOrEmpty(Sucursal.Country))
                Sucursal.Country = "MX";
            if (string.IsNullOrEmpty(Sucursal.OpeningHours))
                Sucursal.OpeningHours = "{\"mon\":[{\"open\":\"09:00\",\"close\":\"17:00\"}],\"tue\":[{\"open\":\"09:00\",\"close\":\"17:00\"}],\"wed\":[{\"open\":\"09:00\",\"close\":\"17:00\"}],\"thu\":[{\"open\":\"09:00\",\"close\":\"17:00\"}],\"fri\":[{\"open\":\"09:00\",\"close\":\"17:00\"}],\"sat\":[{\"open\":\"09:00\",\"close\":\"13:00\"}],\"sun\":[]}";


            // Depuración: Verificar ModelState
            if (!ModelState.IsValid)
            {
                // Agregar información de depuración para ver qué campos están fallando
                foreach (var modelError in ModelState.Where(x => x.Value?.Errors.Count > 0))
                {
                    if (modelError.Value != null)
                    {
                        foreach (var error in modelError.Value.Errors)
                        {
                            Console.WriteLine($"Error en {modelError.Key}: {error.ErrorMessage}");
                        }
                    }
                }
                return Page();
            }

            try
            {
                // Asegurar que los valores por defecto estén configurados
                Sucursal.CreatedAt = DateTime.UtcNow;
                Sucursal.UpdatedAt = null;

                _context.Sucursals.Add(Sucursal);
                await _context.SaveChangesAsync();

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error al crear la sucursal: {ex.Message}");
                return Page();
            }
        }
    }
}
