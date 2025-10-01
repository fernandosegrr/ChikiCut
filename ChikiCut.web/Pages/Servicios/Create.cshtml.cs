using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Attributes;

namespace ChikiCut.web.Pages.Servicios
{
    [RequirePermission("servicios", "create")]
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty]
        public Servicio Servicio { get; set; } = default!;

        [BindProperty]
        public List<string> TagsSeleccionados { get; set; } = new();

        [BindProperty]
        public bool CrearYNuevo { get; set; } = false;

        public IActionResult OnGet()
        {
            // Inicializar servicio con valores predefinidos
            Servicio = new Servicio
            {
                IsActive = true,
                DuracionEstimada = 30,
                PrecioBase = 0.00m,
                RequiereCita = true,
                NivelDificultad = 1,
                ComisionEmpleado = 10.00m,
                Categoria = "Cortes"
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Remover validaciones de campos automáticos
            ModelState.Remove("Servicio.Id");
            ModelState.Remove("Servicio.CreatedAt");
            ModelState.Remove("Servicio.UpdatedAt");
            ModelState.Remove("Servicio.UsuarioCreador");
            ModelState.Remove("Servicio.SucursalesAsignadas");

            // Generar código automáticamente si no se proporciona
            if (string.IsNullOrEmpty(Servicio.Codigo))
            {
                Servicio.Codigo = await GenerarCodigoAutomaticoAsync();
            }

            // Verificar que el código sea único
            var codigoExiste = await _context.Servicios
                .AnyAsync(s => s.Codigo == Servicio.Codigo);

            if (codigoExiste)
            {
                ModelState.AddModelError("Servicio.Codigo", "Ya existe un servicio con este código.");
            }

            // Procesar tags seleccionados
            if (TagsSeleccionados?.Any() == true)
            {
                Servicio.Tags = System.Text.Json.JsonSerializer.Serialize(TagsSeleccionados);
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Obtener usuario actual para auditoría
                var currentUserId = HttpContext.Session.GetString("UserId");
                if (long.TryParse(currentUserId, out var createdBy))
                {
                    Servicio.CreatedBy = createdBy;
                }

                Servicio.CreatedAt = DateTime.UtcNow;
                _context.Servicios.Add(Servicio);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Servicio '{Servicio.Nombre}' creado exitosamente.";

                if (CrearYNuevo)
                {
                    return RedirectToPage("./Create");
                }

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error al crear el servicio: {ex.Message}");
                return Page();
            }
        }

        private async Task<string> GenerarCodigoAutomaticoAsync()
        {
            // Generar código basado en la categoría
            var prefijo = Servicio.Categoria switch
            {
                "Cortes" => "CORTE",
                "Peinados" => "PEIN",
                "Barba" => "BARB",
                "Tratamientos" => "TRAT",
                "Paquetes" => "PAQ",
                "Especiales" => "ESPE",
                _ => "SERV"
            };

            // Obtener el siguiente número para esta categoría
            var ultimoCodigo = await _context.Servicios
                .Where(s => s.Codigo.StartsWith(prefijo))
                .OrderByDescending(s => s.Codigo)
                .Select(s => s.Codigo)
                .FirstOrDefaultAsync();

            var siguienteNumero = 1;
            if (!string.IsNullOrEmpty(ultimoCodigo) && ultimoCodigo.Length >= prefijo.Length + 3)
            {
                var numeroStr = ultimoCodigo.Substring(prefijo.Length);
                if (int.TryParse(numeroStr, out var numero))
                {
                    siguienteNumero = numero + 1;
                }
            }

            return $"{prefijo}{siguienteNumero:D3}";
        }
    }
}