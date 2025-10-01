using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Attributes;

namespace ChikiCut.web.Pages.Servicios
{
    [RequirePermission("servicios", "update")]
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditModel(AppDbContext context) => _context = context;

        [BindProperty]
        public Servicio Servicio { get; set; } = default!;

        [BindProperty]
        public List<string> TagsSeleccionados { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Id == id);
            if (servicio == null)
            {
                return NotFound();
            }

            Servicio = servicio;

            // Cargar tags existentes
            try
            {
                if (!string.IsNullOrEmpty(Servicio.Tags))
                {
                    TagsSeleccionados = System.Text.Json.JsonSerializer.Deserialize<List<string>>(Servicio.Tags) ?? new List<string>();
                }
            }
            catch (System.Text.Json.JsonException)
            {
                TagsSeleccionados = new List<string>();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Remover validaciones de campos automáticos
            ModelState.Remove("Servicio.CreatedAt");
            ModelState.Remove("Servicio.UpdatedAt");
            ModelState.Remove("Servicio.UsuarioCreador");
            ModelState.Remove("Servicio.SucursalesAsignadas");

            // Verificar que el código sea único (excluyendo el servicio actual)
            var codigoExiste = await _context.Servicios
                .AnyAsync(s => s.Codigo == Servicio.Codigo && s.Id != Servicio.Id);

            if (codigoExiste)
            {
                ModelState.AddModelError("Servicio.Codigo", "Ya existe otro servicio con este código.");
            }

            // Procesar tags seleccionados
            if (TagsSeleccionados?.Any() == true)
            {
                Servicio.Tags = System.Text.Json.JsonSerializer.Serialize(TagsSeleccionados);
            }
            else
            {
                Servicio.Tags = "[]";
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var entity = await _context.Servicios.FindAsync(Servicio.Id);
                if (entity == null)
                {
                    return NotFound();
                }

                // Actualizar campos editables
                entity.Codigo = Servicio.Codigo;
                entity.Nombre = Servicio.Nombre;
                entity.Descripcion = Servicio.Descripcion;
                entity.Categoria = Servicio.Categoria;
                entity.Subcategoria = Servicio.Subcategoria;
                entity.DuracionEstimada = Servicio.DuracionEstimada;
                entity.PrecioBase = Servicio.PrecioBase;
                entity.RequiereCita = Servicio.RequiereCita;
                entity.NivelDificultad = Servicio.NivelDificultad;
                entity.ComisionEmpleado = Servicio.ComisionEmpleado;
                entity.NotasInternas = Servicio.NotasInternas;
                entity.ImagenUrl = Servicio.ImagenUrl;
                entity.Tags = Servicio.Tags;
                entity.IsActive = Servicio.IsActive;
                entity.UpdatedAt = DateTime.UtcNow;

                _context.Update(entity);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Servicio '{Servicio.Nombre}' actualizado exitosamente.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error al actualizar el servicio: {ex.Message}");
                return Page();
            }
        }
    }
}