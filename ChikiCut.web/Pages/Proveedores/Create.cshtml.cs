using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Attributes;

namespace ChikiCut.web.Pages.Proveedores
{
    [RequirePermission("proveedores", "create")]
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty]
        public Proveedor Proveedor { get; set; } = default!;

        [BindProperty]
        public List<string> TiposProductosSeleccionados { get; set; } = new();

        public List<string> TiposProductosDisponibles { get; set; } = new()
        {
            "Shampoos", "Acondicionadores", "Productos de styling", "Herramientas",
            "Vinchas", "Moños", "Clips", "Diademas", "Accesorios decorativos",
            "Tintes temporales", "Productos químicos", "Tratamientos capilares",
            "Juguetes", "Premios", "Stickers", "Dulces", "Sorpresas",
            "Sillas infantiles", "Mobiliario", "Decoración", "Espejos temáticos",
            "Equipo de peluquería", "Consumibles", "Productos de limpieza"
        };

        public List<string> CategoriasDisponibles { get; set; } = new()
        {
            "Productos de belleza", "Accesorios infantiles", "Productos químicos",
            "Juguetes y premios", "Mobiliario", "Equipo profesional", "Consumibles", "General"
        };

        public IActionResult OnGet()
        {
            Proveedor = new Proveedor
            {
                IsActive = true,
                Pais = "MX",
                Moneda = "MXN",
                CondicionesPago = "Contado",
                DiasCredito = 0,
                Categoria = "General"
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Remover validaciones de campos automáticos
            ModelState.Remove("Proveedor.Id");
            ModelState.Remove("Proveedor.CreatedAt");
            ModelState.Remove("Proveedor.UpdatedAt");
            ModelState.Remove("Proveedor.CreatedBy");
            ModelState.Remove("Proveedor.UpdatedBy");

            // Validaciones personalizadas
            if (string.IsNullOrEmpty(Proveedor.CodigoProveedor))
            {
                Proveedor.CodigoProveedor = await GenerateProviderCodeAsync();
            }
            else
            {
                if (await _context.Proveedores.AnyAsync(p => p.CodigoProveedor == Proveedor.CodigoProveedor))
                {
                    ModelState.AddModelError("Proveedor.CodigoProveedor", "Ya existe un proveedor con este código.");
                }
            }

            // Validar RFC único si se proporciona
            if (!string.IsNullOrEmpty(Proveedor.Rfc))
            {
                if (await _context.Proveedores.AnyAsync(p => p.Rfc == Proveedor.Rfc))
                {
                    ModelState.AddModelError("Proveedor.Rfc", "Ya existe un proveedor con este RFC.");
                }
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Configurar tipos de productos
                Proveedor.ListaTiposProductos = TiposProductosSeleccionados ?? new List<string>();

                // Configurar campos de auditoría
                Proveedor.CreatedAt = DateTime.UtcNow;
                
                var currentUserId = HttpContext.Session.GetString("UserId");
                if (long.TryParse(currentUserId, out var userId))
                {
                    Proveedor.CreatedBy = userId;
                }

                _context.Proveedores.Add(Proveedor);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Proveedor '{Proveedor.NombreComercial}' creado exitosamente.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error al crear el proveedor: {ex.Message}");
                return Page();
            }
        }

        private async Task<string> GenerateProviderCodeAsync()
        {
            var lastProvider = await _context.Proveedores
                .Where(p => p.CodigoProveedor != null && p.CodigoProveedor.StartsWith("PROV"))
                .OrderByDescending(p => p.CodigoProveedor)
                .FirstOrDefaultAsync();

            var nextNumber = 1;
            if (lastProvider != null && lastProvider.CodigoProveedor != null)
            {
                var numberStr = lastProvider.CodigoProveedor.Substring(4);
                if (int.TryParse(numberStr, out var number))
                {
                    nextNumber = number + 1;
                }
            }

            return $"PROV{nextNumber:D3}";
        }
    }
}