using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Services;
using ChikiCut.web.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ChikiCut.web.Pages.Operaciones.Gastos
{
    public class CreateModel : PageModel
    {
        private readonly IGastoService _gastoService;
        private readonly PermissionHelper _permissionHelper;
        private readonly AppDbContext _context;

        public CreateModel(IGastoService gastoService, PermissionHelper permissionHelper, AppDbContext context)
        {
            _gastoService = gastoService;
            _permissionHelper = permissionHelper;
            _context = context;
        }

        [BindProperty]
        public Gasto Gasto { get; set; } = new();
        public List<Sucursal> SucursalesAsignadas { get; set; } = new();
        public List<ConceptoGasto> Conceptos { get; set; } = new();
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public List<Gasto> GastosFiltrados { get; set; } = new();
        public List<Sucursal> AllSucursales { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _permissionHelper.GetUserId();
            SucursalesAsignadas = await _context.UsuarioSucursales
                .Where(us => us.UsuarioId == userId && us.IsActive && us.Sucursal.IsActive)
                .Select(us => us.Sucursal)
                .ToListAsync();
            // Incluye TODAS las sucursales y conceptos activos para el grid
            AllSucursales = await _context.Sucursals.Where(s => s.IsActive).ToListAsync();
            Conceptos = await _context.ConceptosGasto.Where(cg => cg.IsActive).OrderBy(cg => cg.Nombre).ToListAsync();
            GastosFiltrados = await _context.Gastos.OrderByDescending(g => g.Id).ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAddGastoAsync()
        {
            // Incluye TODAS las sucursales y conceptos activos para el grid
            Conceptos = await _context.ConceptosGasto.Where(cg => cg.IsActive).OrderBy(cg => cg.Nombre).ToListAsync();
            SucursalesAsignadas = await _context.UsuarioSucursales
                .Where(us => us.UsuarioId == _permissionHelper.GetUserId() && us.IsActive && us.Sucursal.IsActive)
                .Select(us => us.Sucursal)
                .ToListAsync();
            AllSucursales = await _context.Sucursals.Where(s => s.IsActive).ToListAsync();
            var log = $"[AddGasto] SucursalId: {Gasto.SucursalId}, ConceptoId: {Gasto.ConceptoGastoId}, Monto: {Gasto.Monto}, Fecha: {Gasto.Fecha}, MetodoPago: {Gasto.MetodoPago}, Descripcion: {Gasto.Descripcion}";
            System.Diagnostics.Debug.WriteLine(log);
            if (!ModelState.IsValid)
            {
                // Log detallado de errores de ModelState
                var errorList = new List<string>();
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    foreach (var error in state.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"ModelState Error - Campo: {key}, Error: {error.ErrorMessage}");
                        errorList.Add($"{key}: {error.ErrorMessage}");
                    }
                }
                ErrorMessage = "Datos inválidos. Verifica los campos.";
                if (errorList.Count > 0)
                {
                    ErrorMessage += "<ul>" + string.Join("", errorList.Select(e => $"<li>{e}</li>")) + "</ul>";
                }
                GastosFiltrados = await _context.Gastos.OrderByDescending(g => g.Id).ToListAsync();
                AllSucursales = await _context.Sucursals.Where(s => s.IsActive).ToListAsync();
                return Partial("_GastosGrid", this);
            }
            if (Gasto.SucursalId == 0 || Gasto.ConceptoGastoId == 0 || Gasto.Monto <= 0)
            {
                ErrorMessage = "Faltan datos obligatorios para el gasto.";
                GastosFiltrados = await _context.Gastos.OrderByDescending(g => g.Id).ToListAsync();
                AllSucursales = await _context.Sucursals.Where(s => s.IsActive).ToListAsync();
                return Partial("_GastosGrid", this);
            }
            // Obtener la fecha local de México (CDMX)
            var mexicoTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            var nowInMexico = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mexicoTimeZone);
            Gasto.Fecha = DateOnly.FromDateTime(nowInMexico);
            Gasto.CreadoEn = DateTime.UtcNow;
            Gasto.Estatus = "activo";
            Gasto.CreadoPor = _permissionHelper.GetUserId();
            await _gastoService.AddAsync(Gasto);
            SuccessMessage = "Gasto registrado exitosamente.";
            GastosFiltrados = await _context.Gastos.OrderByDescending(g => g.Id).ToListAsync();
            AllSucursales = await _context.Sucursals.Where(s => s.IsActive).ToListAsync();
            // Inicializa Gasto para limpiar el formulario, excepto sucursal
            var sucursalIdAnterior = Gasto.SucursalId;
            Gasto = new Gasto { SucursalId = sucursalIdAnterior };
            return Partial("_GastosGrid", this);
        }

        [HttpGet]
        public async Task<IActionResult> OnGetGetGastoAsync(long id)
        {
            var gasto = await _context.Gastos.FirstOrDefaultAsync(g => g.Id == id);
            if (gasto == null)
                return NotFound();
            return new JsonResult(new
            {
                id = gasto.Id,
                sucursalId = gasto.SucursalId,
                conceptoGastoId = gasto.ConceptoGastoId,
                monto = gasto.Monto,
                metodoPago = gasto.MetodoPago,
                descripcion = gasto.Descripcion,
                observaciones = gasto.Observaciones,
                fecha = gasto.Fecha.ToString("yyyy-MM-dd")
            });
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostEditGastoAsync()
        {
            foreach (var key in Request.Form.Keys)
            {
                System.Diagnostics.Debug.WriteLine($"Form[{key}] = {Request.Form[key]}");
            }
            System.Diagnostics.Debug.WriteLine($"RAW: editGastoId='{Request.Form["editGastoId"]}', editSucursalId='{Request.Form["editSucursalId"]}', editConceptoId='{Request.Form["editConceptoId"]}', editMonto='{Request.Form["editMonto"]}'");
            var idStr = Request.Form["editGastoId"];
            var sucursalIdStr = Request.Form["editSucursalId"];
            var conceptoIdStr = Request.Form["editConceptoId"];
            var montoStr = Request.Form["editMonto"];
            var metodoPago = Request.Form["editMetodoPago"];
            var descripcion = Request.Form["editDescripcion"];
            var observaciones = Request.Form["editObservaciones"];

            if (string.IsNullOrWhiteSpace(idStr) || string.IsNullOrWhiteSpace(sucursalIdStr) || string.IsNullOrWhiteSpace(conceptoIdStr) || string.IsNullOrWhiteSpace(montoStr))
            {
                AllSucursales = await _context.Sucursals.Where(s => s.IsActive).ToListAsync();
                return Partial("_GastosGrid", this);
            }
            if (!long.TryParse(idStr, out var id) ||
                !long.TryParse(sucursalIdStr, out var sucursalId) ||
                !long.TryParse(conceptoIdStr, out var conceptoId) ||
                !decimal.TryParse(montoStr, out var monto))
            {
                AllSucursales = await _context.Sucursals.Where(s => s.IsActive).ToListAsync();
                return Partial("_GastosGrid", this);
            }

            var gasto = await _context.Gastos.FirstOrDefaultAsync(g => g.Id == id);
            if (gasto == null)
            {
                AllSucursales = await _context.Sucursals.Where(s => s.IsActive).ToListAsync();
                return Partial("_GastosGrid", this);
            }

            gasto.SucursalId = sucursalId;
            gasto.ConceptoGastoId = conceptoId;
            gasto.Monto = monto;
            gasto.MetodoPago = string.IsNullOrEmpty(metodoPago) ? "" : metodoPago.ToString();
            gasto.Descripcion = string.IsNullOrEmpty(descripcion) ? "" : descripcion.ToString();
            gasto.Observaciones = string.IsNullOrEmpty(observaciones) ? "" : observaciones.ToString();
            // Asignar fecha local de México (CDMX) siempre al editar
            var mexicoTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            var nowInMexico = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mexicoTimeZone);
            gasto.Fecha = DateOnly.FromDateTime(nowInMexico);
            gasto.UpdatedEn = DateTime.UtcNow;
            gasto.Estatus = "activo";

            await _context.SaveChangesAsync();
            GastosFiltrados = await _context.Gastos.OrderByDescending(g => g.Id).ToListAsync();
            AllSucursales = await _context.Sucursals.Where(s => s.IsActive).ToListAsync();
            Conceptos = await _context.ConceptosGasto.Where(cg => cg.IsActive).OrderBy(cg => cg.Nombre).ToListAsync();
            return Partial("_GastosGrid", this);
        }

        [IgnoreAntiforgeryToken]
        public IActionResult OnPostEditGastoTest()
        {
            return new JsonResult(new { success = true, message = "Handler invocado correctamente" });
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostActualizarGastoAsync()
        {
            foreach (var key in Request.Form.Keys)
            {
                System.Diagnostics.Debug.WriteLine($"Form[{key}] = {Request.Form[key]}");
            }
            System.Diagnostics.Debug.WriteLine($"RAW: editGastoId='{Request.Form["editGastoId"]}', editSucursalId='{Request.Form["editSucursalId"]}', editConceptoId='{Request.Form["editConceptoId"]}', editMonto='{Request.Form["editMonto"]}'");
            var idStr = Request.Form["editGastoId"];
            var sucursalIdStr = Request.Form["editSucursalId"];
            var conceptoIdStr = Request.Form["editConceptoId"];
            var montoStr = Request.Form["editMonto"];
            var metodoPago = Request.Form["editMetodoPago"];
            var descripcion = Request.Form["editDescripcion"];
            var observaciones = Request.Form["editObservaciones"];

            if (string.IsNullOrWhiteSpace(idStr) || string.IsNullOrWhiteSpace(sucursalIdStr) || string.IsNullOrWhiteSpace(conceptoIdStr) || string.IsNullOrWhiteSpace(montoStr))
            {
                AllSucursales = await _context.Sucursals.Where(s => s.IsActive).ToListAsync();
                return Partial("_GastosGrid", this);
            }
            if (!long.TryParse(idStr, out var id) ||
                !long.TryParse(sucursalIdStr, out var sucursalId) ||
                !long.TryParse(conceptoIdStr, out var conceptoId) ||
                !decimal.TryParse(montoStr, out var monto))
            {
                AllSucursales = await _context.Sucursals.Where(s => s.IsActive).ToListAsync();
                return Partial("_GastosGrid", this);
            }

            var gasto = await _context.Gastos.FirstOrDefaultAsync(g => g.Id == id);
            if (gasto == null)
            {
                AllSucursales = await _context.Sucursals.Where(s => s.IsActive).ToListAsync();
                return Partial("_GastosGrid", this);
            }

            gasto.SucursalId = sucursalId;
            gasto.ConceptoGastoId = conceptoId;
            gasto.Monto = monto;
            gasto.MetodoPago = string.IsNullOrEmpty(metodoPago) ? "" : metodoPago.ToString();
            gasto.Descripcion = string.IsNullOrEmpty(descripcion) ? "" : descripcion.ToString();
            gasto.Observaciones = string.IsNullOrEmpty(observaciones) ? "" : observaciones.ToString();
            gasto.UpdatedEn = DateTime.UtcNow;
            gasto.Estatus = "activo";

            await _context.SaveChangesAsync();
            GastosFiltrados = await _context.Gastos.OrderByDescending(g => g.Id).ToListAsync();
            AllSucursales = await _context.Sucursals.Where(s => s.IsActive).ToListAsync();
            Conceptos = await _context.ConceptosGasto.Where(cg => cg.IsActive).OrderBy(cg => cg.Nombre).ToListAsync();
            return Partial("_GastosGrid", this);
        }

        public async Task<PartialViewResult> OnGetGastosGridAsync()
        {
            var gastos = await _context.Gastos.OrderByDescending(g => g.Id).ToListAsync();
            var conceptos = await _context.ConceptosGasto.Where(cg => cg.IsActive).OrderBy(cg => cg.Nombre).ToListAsync();
            var sucursales = await _context.Sucursals.Where(s => s.IsActive).ToListAsync();
            ViewData["Conceptos"] = conceptos;
            ViewData["Sucursales"] = sucursales;
            return Partial("_GastosGrid", gastos);
        }

        public async Task<PartialViewResult> OnGetGastosGridPartialAsync()
        {
            var rows = await GetGridRowsAsync();
            return Partial("_GastosGrid", rows);
        }

        public async Task<IReadOnlyList<GastoGridRowVm>> GetGridRowsAsync()
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            return await (
                from g in _context.Gastos.AsNoTracking()
                where g.Fecha == hoy
                join s in _context.Sucursals.AsNoTracking() on g.SucursalId equals s.Id into sj
                from s in sj.DefaultIfEmpty()
                join c in _context.ConceptosGasto.AsNoTracking() on g.ConceptoGastoId equals c.Id into cj
                from c in cj.DefaultIfEmpty()
                orderby g.Fecha descending, g.Id descending
                select new GastoGridRowVm(
                    g.Id,
                    s != null ? s.Name : $"ID:{g.SucursalId}",
                    c != null ? c.Nombre : $"ID:{g.ConceptoGastoId}",
                    g.Monto,
                    g.MetodoPago ?? string.Empty,
                    g.Descripcion ?? string.Empty,
                    g.Observaciones ?? string.Empty,
                    g.Fecha,
                    null // ComprobanteUrl, no se usa en Create
                )
            ).ToListAsync();
        }
    }
}
