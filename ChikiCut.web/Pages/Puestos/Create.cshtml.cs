using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;

namespace ChikiCut.web.Pages.Puestos
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context) => _context = context;

        public IActionResult OnGet()
        {
            // Inicializar puesto con valores predefinidos
            Puesto = new Puesto
            {
                IsActive = true,
                NivelJerarquico = 1,
                RequiereExperiencia = false
            };

            return Page();
        }

        [BindProperty]
        public Puesto Puesto { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            // Remover validaciones de campos automáticos
            ModelState.Remove("Puesto.Id");
            ModelState.Remove("Puesto.CreatedAt");
            ModelState.Remove("Puesto.UpdatedAt");
            ModelState.Remove("Puesto.Empleados");

            // Validación personalizada de rangos salariales
            if (Puesto.SalarioBaseMinimo.HasValue && Puesto.SalarioBaseMaximo.HasValue)
            {
                if (Puesto.SalarioBaseMinimo > Puesto.SalarioBaseMaximo)
                {
                    ModelState.AddModelError("Puesto.SalarioBaseMaximo", "El salario máximo debe ser mayor o igual al salario mínimo.");
                }
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                Puesto.CreatedAt = DateTime.UtcNow;
                _context.Puestos.Add(Puesto);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Puesto '{Puesto.Nombre}' creado exitosamente.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error al crear el puesto: {ex.Message}");
                return Page();
            }
        }
    }
}