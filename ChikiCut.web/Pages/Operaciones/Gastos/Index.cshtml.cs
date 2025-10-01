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
using System.IO;
using ClosedXML.Excel;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using QuestPDF.Elements;

namespace ChikiCut.web.Pages.Operaciones.Gastos
{
    public class IndexModel : PageModel
    {
        private readonly IGastoService _gastoService;
        private readonly PermissionHelper _permissionHelper;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public IndexModel(IGastoService gastoService, PermissionHelper permissionHelper, AppDbContext context, IWebHostEnvironment env)
        {
            _gastoService = gastoService;
            _permissionHelper = permissionHelper;
            _context = context;
            _env = env;
        }

        [BindProperty]
        public Gasto Gasto { get; set; } = new();
        public List<Sucursal> SucursalesAsignadas { get; set; } = new();
        public List<ConceptoGasto> Conceptos { get; set; } = new();
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public List<Gasto> GastosFiltrados { get; set; } = new();
        public List<Sucursal> AllSucursales { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = _permissionHelper.GetUserId();
            SucursalesAsignadas = await _context.UsuarioSucursales
                .Where(us => us.UsuarioId == userId && us.IsActive && us.Sucursal.IsActive)
                .Select(us => us.Sucursal)
                .ToListAsync();
            AllSucursales = await _context.Sucursals.Where(s => s.IsActive).ToListAsync();
            Conceptos = await _context.ConceptosGasto.Where(cg => cg.IsActive).OrderBy(cg => cg.Nombre).ToListAsync();
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
                join comp in _context.Comprobantes.AsNoTracking() on g.ComprobanteId equals comp.Id into compj
                from comp in compj.DefaultIfEmpty()
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
                    comp != null ? comp.ArchivoUrl : null // ComprobanteUrl
                )
            ).ToListAsync();
        }

        // Handler para devolver solo el grid como partial (evita duplicación)
        public async Task<PartialViewResult> OnGetGastosGridPartialAsync()
        {
            var rows = await GetGridRowsAsync();
            return Partial("_GastosGrid", rows);
        }

        // Handler para devolver un gasto por id en JSON para el modal de edición
        public async Task<IActionResult> OnGetGetGastoAsync(int id)
        {
            var gasto = await _context.Gastos.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
            if (gasto == null) return NotFound();
            string? comprobanteUrl = null;
            if (gasto.ComprobanteId.HasValue)
            {
                comprobanteUrl = await _context.Comprobantes
                    .Where(c => c.Id == gasto.ComprobanteId.Value)
                    .Select(c => c.ArchivoUrl)
                    .FirstOrDefaultAsync();
            }
            return new JsonResult(new {
                id = gasto.Id,
                monto = gasto.Monto,
                descripcion = gasto.Descripcion,
                observaciones = gasto.Observaciones,
                sucursalId = gasto.SucursalId,
                conceptoGastoId = gasto.ConceptoGastoId,
                metodoPago = gasto.MetodoPago,
                fecha = gasto.Fecha.ToString("yyyy-MM-dd"),
                comprobanteUrl = comprobanteUrl
            });
        }

        // Handler para reporte de gastos filtrado por sucursal y fecha
        public async Task<PartialViewResult> OnGetReporteGastosAsync(int? sucursalId, DateTime? fecha)
        {
            var gastosQuery = _context.Gastos.AsNoTracking();
            if (sucursalId.HasValue && sucursalId.Value > 0)
                gastosQuery = gastosQuery.Where(g => g.SucursalId == sucursalId.Value);
            if (fecha.HasValue)
                gastosQuery = gastosQuery.Where(g => g.Fecha == DateOnly.FromDateTime(fecha.Value));

            // Cargar ids relacionados
            var sucursalesDict = _context.Sucursals.AsNoTracking().ToDictionary(s => s.Id, s => s.Name);
            var conceptosDict = _context.ConceptosGasto.AsNoTracking().ToDictionary(c => c.Id, c => c.Nombre);
            var comprobantesDict = _context.Comprobantes.AsNoTracking().ToDictionary(c => c.Id, c => c.ArchivoUrl);

            var gastosList = await gastosQuery.ToListAsync();

            var rows = gastosList
                .Select(g => new ChikiCut.web.Pages.Operaciones.Gastos.GastoGridRowVm(
                    g.Id,
                    sucursalesDict.TryGetValue(g.SucursalId, out var sucName) ? sucName : $"ID:{g.SucursalId}",
                    conceptosDict.TryGetValue(g.ConceptoGastoId, out var conName) ? conName : $"ID:{g.ConceptoGastoId}",
                    g.Monto,
                    g.MetodoPago ?? string.Empty,
                    g.Descripcion ?? string.Empty,
                    g.Observaciones ?? string.Empty,
                    g.Fecha,
                    g.ComprobanteId.HasValue && comprobantesDict.TryGetValue(g.ComprobanteId.Value, out var compUrl) ? compUrl : null
                ))
                .OrderByDescending(x => x.Fecha)
                .ThenByDescending(x => x.Id)
                .ToList();

            return Partial("_ReporteGastosGrid", rows);
        }

        // Exportar a Excel SOLO los datos filtrados y sin campo comprobante
        public async Task<IActionResult> OnPostExportarExcelAsync(DateTime? fecha, int? sucursalId)
        {
            var rows = await GetReporteRowsAsync(sucursalId, fecha);
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Gastos");
            // Encabezados con formato
            var headers = new[] { "Fecha", "Sucursal", "Concepto", "Monto", "Método de Pago", "Descripción", "Observaciones" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
                ws.Cell(1, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(1, i + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            int row = 2;
            foreach (var g in rows)
            {
                ws.Cell(row, 1).Value = g.Fecha.ToString("dd/MM/yyyy");
                ws.Cell(row, 2).Value = g.SucursalNombre;
                ws.Cell(row, 3).Value = g.ConceptoNombre;
                ws.Cell(row, 4).Value = g.Monto;
                ws.Cell(row, 4).Style.NumberFormat.Format = "$ #,##0.00";
                ws.Cell(row, 5).Value = g.MetodoPago;
                ws.Cell(row, 6).Value = g.Descripcion;
                ws.Cell(row, 7).Value = g.Observaciones;
                for (int col = 1; col <= 7; col++)
                {
                    ws.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    ws.Cell(row, col).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                row++;
            }
            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1); // Congela encabezado
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Gastos.xlsx");
        }

        // Exportar a PDF usando QuestPDF con columnas horizontales y sin saltos de línea
        public async Task<IActionResult> OnPostExportarPdfAsync(DateTime? fecha, int? sucursalId)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var rows = await GetReporteRowsAsync(sucursalId, fecha);
            var stream = new MemoryStream();
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A4.Landscape());
                    page.Header().Text("Reporte de Gastos").FontSize(18).Bold().AlignCenter();
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80); // Fecha
                            columns.ConstantColumn(120); // Sucursal
                            columns.ConstantColumn(90); // Concepto
                            columns.ConstantColumn(70); // Monto
                            columns.ConstantColumn(90); // Método
                            columns.ConstantColumn(120); // Descripción
                            columns.ConstantColumn(120); // Observaciones
                        });
                        // Encabezados
                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Fecha").Bold();
                            header.Cell().Element(CellStyle).Text("Sucursal").Bold();
                            header.Cell().Element(CellStyle).Text("Concepto").Bold();
                            header.Cell().Element(CellStyle).Text("Monto").Bold();
                            header.Cell().Element(CellStyle).Text("Método de Pago").Bold();
                            header.Cell().Element(CellStyle).Text("Descripción").Bold();
                            header.Cell().Element(CellStyle).Text("Observaciones").Bold();
                        });
                        // Filas
                        foreach (var g in rows)
                        {
                            table.Cell().Element(CellStyle).Text(g.Fecha.ToString("dd/MM/yyyy")).LineHeight(1).FontSize(9);
                            table.Cell().Element(CellStyle).Text(g.SucursalNombre).LineHeight(1).FontSize(9);
                            table.Cell().Element(CellStyle).Text(g.ConceptoNombre).LineHeight(1).FontSize(9);
                            table.Cell().Element(CellStyle).Text(g.Monto.ToString("C")).LineHeight(1).FontSize(9);
                            table.Cell().Element(CellStyle).Text(g.MetodoPago).LineHeight(1).FontSize(9);
                            table.Cell().Element(CellStyle).Text(g.Descripcion).LineHeight(1).FontSize(9);
                            table.Cell().Element(CellStyle).Text(g.Observaciones).LineHeight(1).FontSize(9);
                        }
                        static IContainer CellStyle(IContainer container) => container.PaddingVertical(2).PaddingHorizontal(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).ShowOnce();
                    });
                });
            });
            doc.GeneratePdf(stream);
            stream.Position = 0;
            return File(stream.ToArray(), "application/pdf", "Gastos.pdf");
        }

        // Utilidad para obtener los rows del reporte
        private async Task<List<GastoGridRowVm>> GetReporteRowsAsync(int? sucursalId, DateTime? fecha)
        {
            var gastosQuery = _context.Gastos.AsNoTracking();
            if (sucursalId.HasValue && sucursalId.Value > 0)
                gastosQuery = gastosQuery.Where(g => g.SucursalId == sucursalId.Value);
            if (fecha.HasValue)
                gastosQuery = gastosQuery.Where(g => g.Fecha == DateOnly.FromDateTime(fecha.Value));
            var sucursalesDict = _context.Sucursals.AsNoTracking().ToDictionary(s => s.Id, s => s.Name);
            var conceptosDict = _context.ConceptosGasto.AsNoTracking().ToDictionary(c => c.Id, c => c.Nombre);
            var comprobantesDict = _context.Comprobantes.AsNoTracking().ToDictionary(c => c.Id, c => c.ArchivoUrl);
            var gastosList = await gastosQuery.ToListAsync();
            return gastosList
                .Select(g => new GastoGridRowVm(
                    g.Id,
                    sucursalesDict.TryGetValue(g.SucursalId, out var sucName) ? sucName : $"ID:{g.SucursalId}",
                    conceptosDict.TryGetValue(g.ConceptoGastoId, out var conName) ? conName : $"ID:{g.ConceptoGastoId}",
                    g.Monto,
                    g.MetodoPago ?? string.Empty,
                    g.Descripcion ?? string.Empty,
                    g.Observaciones ?? string.Empty,
                    g.Fecha,
                    g.ComprobanteId.HasValue && comprobantesDict.TryGetValue(g.ComprobanteId.Value, out var compUrl) ? compUrl : null
                ))
                .OrderByDescending(x => x.Fecha)
                .ThenByDescending(x => x.Id)
                .ToList();
        }
    }
}
