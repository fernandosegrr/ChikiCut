using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Extensions;

namespace ChikiCut.web.Pages.ConceptosGasto
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context)
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

        public async Task<IActionResult> OnGetAsync(long? duplicateFrom)
        {
            await CargarDatosAsync();

            // Inicializar nuevo concepto
            ConceptoGasto = new ConceptoGasto
            {
                IsActive = true,
                RequiereComprobante = true,
                AplicaTodasSucursales = true,
                NivelAutorizacionRequerido = 1,
                TipoFrecuencia = "Ocasional",
                Categoria = "General"
            };

            // Si se está duplicando desde otro concepto
            if (duplicateFrom.HasValue)
            {
                var conceptoOriginal = await _context.ConceptosGasto
                    .FirstOrDefaultAsync(c => c.Id == duplicateFrom.Value);

                if (conceptoOriginal != null)
                {
                    ConceptoGasto.Nombre = $"Copia de {conceptoOriginal.Nombre}";
                    ConceptoGasto.Descripcion = conceptoOriginal.Descripcion;
                    ConceptoGasto.Categoria = conceptoOriginal.Categoria;
                    ConceptoGasto.Subcategoria = conceptoOriginal.Subcategoria;
                    ConceptoGasto.TipoFrecuencia = conceptoOriginal.TipoFrecuencia;
                    ConceptoGasto.RequiereComprobante = conceptoOriginal.RequiereComprobante;
                    ConceptoGasto.LimiteMaximo = conceptoOriginal.LimiteMaximo;
                    ConceptoGasto.RequiereAutorizacion = conceptoOriginal.RequiereAutorizacion;
                    ConceptoGasto.NivelAutorizacionRequerido = conceptoOriginal.NivelAutorizacionRequerido;
                    ConceptoGasto.CuentaContable = conceptoOriginal.CuentaContable;
                    ConceptoGasto.AplicaTodasSucursales = conceptoOriginal.AplicaTodasSucursales;
                    
                    TagsInput = conceptoOriginal.TagsList;
                    SucursalesSeleccionadas = conceptoOriginal.SucursalesAplicablesList;
                    
                    // Generar nuevo código basado en la categoría
                    ConceptoGasto.Codigo = GenerarCodigo(conceptoOriginal.Categoria);
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await CargarDatosAsync();
                return Page();
            }

            // Validar código único
            var existeCodigo = await _context.ConceptosGasto
                .AnyAsync(c => c.Codigo == ConceptoGasto.Codigo);

            if (existeCodigo)
            {
                ModelState.AddModelError("ConceptoGasto.Codigo", "Ya existe un concepto con este código.");
                await CargarDatosAsync();
                return Page();
            }

            // Configurar fechas
            ConceptoGasto.CreatedAt = DateTime.UtcNow;
            
            // TODO: Obtener el ID del usuario actual de la sesión
            // ConceptoGasto.CreatedBy = HttpContext.Session.GetInt64("UserId");

            // Configurar tags
            if (TagsInput.Any())
            {
                ConceptoGasto.TagsList = TagsInput.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            }

            // Configurar sucursales aplicables
            if (!ConceptoGasto.AplicaTodasSucursales && SucursalesSeleccionadas.Any())
            {
                ConceptoGasto.SucursalesAplicablesList = SucursalesSeleccionadas;
            }
            else if (ConceptoGasto.AplicaTodasSucursales)
            {
                ConceptoGasto.SucursalesAplicablesList = new List<long>();
            }

            _context.ConceptosGasto.Add(ConceptoGasto);
            await _context.SaveChangesUtcAsync();

            // Verificar si se quiere crear otro
            if (Request.Form.ContainsKey("CreateAndNew"))
            {
                return RedirectToPage("./Create");
            }

            return RedirectToPage("./Details", new { id = ConceptoGasto.Id });
        }

        private async Task CargarDatosAsync()
        {
            SucursalesDisponibles = await _context.Sucursals
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        private string GenerarCodigo(string categoria)
        {
            var prefijos = new Dictionary<string, string>
            {
                { "Servicios", "SERV" },
                { "Insumos", "INSUM" },
                { "Mantenimiento", "MANT" },
                { "Administrativo", "ADMIN" },
                { "Marketing", "MARK" },
                { "Recursos Humanos", "RRHH" },
                { "Varios", "VAR" }
            };

            var prefijo = prefijos.GetValueOrDefault(categoria, "GEN");
            var numero = new Random().Next(1, 1000).ToString("D3");
            
            return $"{prefijo}{numero}";
        }
    }
}