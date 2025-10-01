using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Attributes;

namespace ChikiCut.web.Pages.Productos
{
    [RequirePermission("productos", "create")]
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty]
        public Producto Producto { get; set; } = default!;

        [BindProperty]
        public List<string> TagsSeleccionados { get; set; } = new();

        [BindProperty]
        public List<string> ImagenesAdicionales { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            Producto = new Producto
            {
                // Valores por defecto
                TipoProducto = "Físico",
                UnidadMedida = "Pieza",
                PrecioBase = 0,
                MargenGananciaSugerido = 30,
                RequiereInventario = true,
                StockMinimo = 5,
                StockMaximo = 100,
                PuntoReorden = 10,
                EsPerecedero = false,
                EsControlado = false,
                TiempoEntregaDias = 7,
                EsDestacado = false,
                EsNovedad = false,
                DescuentoMaximo = 10,
                ComisionVenta = 5,
                IsActive = true,
                TemperaturaAlmacenamiento = "Ambiente"
            };

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

            // Verificar que el código sea único
            var codigoExiste = await _context.Productos.AnyAsync(p => p.Codigo == Producto.Codigo);
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
                // Obtener ID del usuario actual
                var userId = HttpContext.Session.GetString("UserId");
                if (long.TryParse(userId, out var usuarioId))
                {
                    Producto.CreatedBy = usuarioId;
                }

                Producto.CreatedAt = DateTime.UtcNow;
                Producto.ConfiguracionAdicional = "{}";

                _context.Productos.Add(Producto);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Producto '{Producto.Nombre}' creado exitosamente. Código: {Producto.Codigo}";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error al crear el producto: {ex.Message}");
                return Page();
            }
        }

        public async Task<IActionResult> OnGetValidateCodigoAsync(string codigo)
        {
            if (string.IsNullOrEmpty(codigo))
            {
                return new JsonResult(new { isValid = false, message = "El código es requerido" });
            }

            var existe = await _context.Productos.AnyAsync(p => p.Codigo == codigo);
            return new JsonResult(new { 
                isValid = !existe, 
                message = existe ? "Este código ya está en uso" : "Código disponible" 
            });
        }

        public IActionResult OnPostGenerateCodigoAsync(string categoria, string marca)
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
    }
}