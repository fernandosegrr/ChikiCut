using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Attributes;

namespace ChikiCut.web.Pages.Productos
{
    [RequirePermission("productos", "update")]
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditModel(AppDbContext context) => _context = context;

        [BindProperty]
        public Producto Producto { get; set; } = default!;

        [BindProperty]
        public List<string> TagsSeleccionados { get; set; } = new();

        [BindProperty]
        public List<string> ImagenesAdicionales { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null)
            {
                return NotFound();
            }

            Producto = producto;

            // Cargar tags existentes
            try
            {
                TagsSeleccionados = System.Text.Json.JsonSerializer.Deserialize<List<string>>(producto.Tags) ?? new List<string>();
            }
            catch
            {
                TagsSeleccionados = new List<string>();
            }

            // Cargar imágenes adicionales existentes
            try
            {
                ImagenesAdicionales = System.Text.Json.JsonSerializer.Deserialize<List<string>>(producto.ImagenesAdicionales) ?? new List<string>();
            }
            catch
            {
                ImagenesAdicionales = new List<string>();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Remover validaciones de campos automáticos
            ModelState.Remove("Producto.CreatedAt");
            ModelState.Remove("Producto.UpdatedAt");
            ModelState.Remove("Producto.ProveedorPrincipal");
            ModelState.Remove("Producto.UsuarioCreador");
            ModelState.Remove("Producto.SucursalesAsignadas");

            // Verificar que el código sea único (excluyendo el producto actual)
            var codigoExiste = await _context.Productos
                .AnyAsync(p => p.Codigo == Producto.Codigo && p.Id != Producto.Id);
            
            if (codigoExiste)
            {
                ModelState.AddModelError("Producto.Codigo", "Ya existe otro producto con este código.");
            }

            // Validaciones de negocio
            if (Producto.StockMaximo < Producto.StockMinimo)
            {
                ModelState.AddModelError("Producto.StockMaximo", "El stock máximo debe ser mayor o igual al stock mínimo.");
            }

            if (Producto.PuntoReorden > Producto.StockMaximo)
            {
                ModelState.AddModelError("Producto.PuntoReorden", "El punto de reorden no puede ser mayor al stock máximo.");
            }

            if (Producto.EsPerecedero && !Producto.VidaUtilDias.HasValue)
            {
                ModelState.AddModelError("Producto.VidaUtilDias", "Los productos perecederos requieren especificar la vida útil.");
            }

            // Procesar tags seleccionados
            if (TagsSeleccionados?.Any() == true)
            {
                Producto.Tags = System.Text.Json.JsonSerializer.Serialize(TagsSeleccionados);
            }
            else
            {
                Producto.Tags = "[]";
            }

            // Procesar imágenes adicionales
            if (ImagenesAdicionales?.Any() == true)
            {
                var imagenesLimpias = ImagenesAdicionales.Where(img => !string.IsNullOrWhiteSpace(img)).ToList();
                Producto.ImagenesAdicionales = System.Text.Json.JsonSerializer.Serialize(imagenesLimpias);
            }
            else
            {
                Producto.ImagenesAdicionales = "[]";
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Obtener el producto existente para preservar campos del sistema
                var existingProducto = await _context.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == Producto.Id);
                if (existingProducto == null)
                {
                    return NotFound();
                }

                // Preservar campos del sistema
                Producto.CreatedAt = existingProducto.CreatedAt;
                Producto.CreatedBy = existingProducto.CreatedBy;
                Producto.UpdatedAt = DateTime.UtcNow;

                // Preservar configuración adicional si no se modificó
                if (string.IsNullOrEmpty(Producto.ConfiguracionAdicional))
                {
                    Producto.ConfiguracionAdicional = existingProducto.ConfiguracionAdicional ?? "{}";
                }

                _context.Attach(Producto).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Producto '{Producto.Nombre}' actualizado exitosamente.";
                return RedirectToPage("./Details", new { id = Producto.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductoExists(Producto.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error al actualizar el producto: {ex.Message}");
                return Page();
            }
        }

        public async Task<IActionResult> OnGetValidateCodigoAsync(string codigo, long id)
        {
            if (string.IsNullOrEmpty(codigo))
            {
                return new JsonResult(new { isValid = false, message = "El código es requerido" });
            }

            var existe = await _context.Productos.AnyAsync(p => p.Codigo == codigo && p.Id != id);
            return new JsonResult(new { 
                isValid = !existe, 
                message = existe ? "Este código ya está en uso" : "Código disponible" 
            });
        }

        public IActionResult OnPostGenerateCodigoAsync(string categoria, string marca, long id)
        {
            try
            {
                var prefijo = "";
                
                // Crear prefijo basado en categoría
                switch (categoria?.ToLower())
                {
                    case "cuidado capilar":
                        prefijo = "CC";
                        break;
                    case "peinado":
                        prefijo = "PE";
                        break;
                    case "tratamientos":
                        prefijo = "TR";
                        break;
                    case "coloración":
                        prefijo = "COL";
                        break;
                    case "herramientas":
                        prefijo = "HER";
                        break;
                    case "accesorios":
                        prefijo = "ACC";
                        break;
                    default:
                        prefijo = "PROD";
                        break;
                }

                // Agregar iniciales de marca si están disponibles
                if (!string.IsNullOrEmpty(marca) && marca.Length >= 2)
                {
                    prefijo += marca.Substring(0, Math.Min(2, marca.Length)).ToUpper();
                }

                // Generar número secuencial
                var timestamp = DateTime.Now.ToString("HHmmss");
                var codigo = $"{prefijo}{timestamp}";

                return new JsonResult(new { success = true, codigo = codigo });
            }
            catch
            {
                return new JsonResult(new { success = false, codigo = "PROD" + DateTime.Now.ToString("HHmmss") });
            }
        }

        private bool ProductoExists(long id)
        {
            return _context.Productos.Any(e => e.Id == id);
        }
    }
}