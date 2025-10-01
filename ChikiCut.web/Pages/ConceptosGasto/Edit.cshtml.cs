using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Extensions;

namespace ChikiCut.web.Pages.ConceptosGasto
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ConceptoGasto ConceptoGasto { get; set; } = default!;

        [BindProperty]
        public List<string> TagsInput { get; set; } = new();

        [BindProperty]
        public List<long> SucursalesSeleccionadas { get; set; } = new();

        public List<Sucursal> SucursalesDisponibles { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var conceptoGasto = await _context.ConceptosGasto
                .Include(c => c.UsuarioCreador)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (conceptoGasto == null)
            {
                return NotFound();
            }

            ConceptoGasto = conceptoGasto;
            TagsInput = ConceptoGasto.TagsList;
            SucursalesSeleccionadas = ConceptoGasto.SucursalesAplicablesList;

            await CargarDatosAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await CargarDatosAsync();
                return Page();
            }

            // Obtener el concepto original de la base de datos
            var conceptoOriginal = await _context.ConceptosGasto
                .FirstOrDefaultAsync(c => c.Id == ConceptoGasto.Id);

            if (conceptoOriginal == null)
            {
                return NotFound();
            }

            // Validar código único (excluyendo el actual)
            var existeCodigo = await _context.ConceptosGasto
                .AnyAsync(c => c.Codigo == ConceptoGasto.Codigo && c.Id != ConceptoGasto.Id);

            if (existeCodigo)
            {
                ModelState.AddModelError("ConceptoGasto.Codigo", "Ya existe otro concepto con este código.");
                await CargarDatosAsync();
                return Page();
            }

            // Actualizar solo los campos necesarios manteniendo las fechas originales
            conceptoOriginal.Codigo = ConceptoGasto.Codigo;
            conceptoOriginal.Nombre = ConceptoGasto.Nombre;
            conceptoOriginal.Descripcion = ConceptoGasto.Descripcion;
            conceptoOriginal.Categoria = ConceptoGasto.Categoria;
            conceptoOriginal.Subcategoria = ConceptoGasto.Subcategoria;
            conceptoOriginal.TipoFrecuencia = ConceptoGasto.TipoFrecuencia;
            conceptoOriginal.RequiereComprobante = ConceptoGasto.RequiereComprobante;
            conceptoOriginal.LimiteMaximo = ConceptoGasto.LimiteMaximo;
            conceptoOriginal.RequiereAutorizacion = ConceptoGasto.RequiereAutorizacion;
            conceptoOriginal.NivelAutorizacionRequerido = ConceptoGasto.NivelAutorizacionRequerido;
            conceptoOriginal.CuentaContable = ConceptoGasto.CuentaContable;
            conceptoOriginal.AplicaTodasSucursales = ConceptoGasto.AplicaTodasSucursales;
            conceptoOriginal.IsActive = ConceptoGasto.IsActive;

            // Configurar fecha de actualización asegurando que sea UTC
            conceptoOriginal.UpdatedAt = DateTime.UtcNow;

            // Configurar tags
            if (TagsInput.Any())
            {
                conceptoOriginal.TagsList = TagsInput.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            }
            else
            {
                conceptoOriginal.TagsList = new List<string>();
            }

            // Configurar sucursales aplicables
            if (!ConceptoGasto.AplicaTodasSucursales && SucursalesSeleccionadas.Any())
            {
                conceptoOriginal.SucursalesAplicablesList = SucursalesSeleccionadas;
            }
            else
            {
                conceptoOriginal.SucursalesAplicablesList = new List<long>();
            }

            try
            {
                // Usar el método de extensión que asegura UTC
                await _context.SaveChangesUtcAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ConceptoGastoExists(ConceptoGasto.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Details", new { id = ConceptoGasto.Id });
        }

        private bool ConceptoGastoExists(long id)
        {
            return _context.ConceptosGasto.Any(e => e.Id == id);
        }

        private async Task CargarDatosAsync()
        {
            SucursalesDisponibles = await _context.Sucursals
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }
    }
}