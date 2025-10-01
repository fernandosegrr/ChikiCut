using Microsoft.AspNetCore.Mvc;
using ChikiCut.web.Data;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Linq;
using ChikiCut.web.Api;
using Microsoft.Extensions.Primitives;

namespace ChikiCut.web.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class GastosApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<GastosApiController> _logger;
        public GastosApiController(AppDbContext context, ILogger<GastosApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("editar")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Editar([FromForm] EditGastoDto dto)
        {
            // 1. Validación de ModelState manual
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(kv => kv.Value?.Errors?.Count > 0)
                    .ToDictionary(kv => kv.Key, kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
                return BadRequest(new
                {
                    success = false,
                    message = "Datos inválidos en la solicitud.",
                    modelErrors = errors
                });
            }

            // 2. Parseo de monto y fecha
            if (!dto.TryParseMonto(out var monto))
            {
                return BadRequest(new { success = false, message = $"Monto inválido: '{dto.MontoRaw}' (usa punto decimal, e.g. 1234.56)" });
            }

            DateOnly fecha;
            if (!string.IsNullOrWhiteSpace(dto.FechaRaw))
            {
                if (!dto.TryParseFecha(out fecha))
                {
                    return BadRequest(new { success = false, message = $"Formato de fecha inválido: '{dto.FechaRaw}' (usa dd/MM/yyyy o yyyy-MM-dd)" });
                }
            }
            else
            {
                // Si no se envía fecha, usar la fecha de hoy
                fecha = DateOnly.FromDateTime(DateTime.Now);
            }

            // 3. Cargar entidad
            var gasto = await _context.Gastos.FirstOrDefaultAsync(g => g.Id == dto.GastoId!.Value);
            if (gasto == null)
                return NotFound(new { success = false, message = "No se encontró el gasto." });

            // 4. Asignar campos (respetando NULL/longitudes)
            gasto.SucursalId = dto.SucursalId!.Value;
            gasto.ConceptoGastoId = dto.ConceptoId!.Value;
            gasto.Monto = monto;
            gasto.MetodoPago = (dto.MetodoPago ?? string.Empty).Trim();
            if (gasto.MetodoPago.Length > 50) gasto.MetodoPago = gasto.MetodoPago.Substring(0, 50);
            gasto.Descripcion = (dto.Descripcion ?? string.Empty).Trim();
            if (gasto.Descripcion.Length > 255) gasto.Descripcion = gasto.Descripcion.Substring(0, 255);
            gasto.Observaciones = (dto.Observaciones ?? string.Empty).Trim();
            if (gasto.Observaciones.Length > 255) gasto.Observaciones = gasto.Observaciones.Substring(0, 255);
            gasto.Fecha = fecha;
            gasto.UpdatedEn = DateTime.UtcNow;
            gasto.Estatus = string.IsNullOrWhiteSpace(gasto.Estatus) ? "activo" : gasto.Estatus;

            // 5. Guardar comprobante si se subió uno nuevo
            if (dto.EditComprobanteFile != null && dto.EditComprobanteFile.Length > 0)
            {
                var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "comprobantes");
                if (!Directory.Exists(uploadsRoot)) Directory.CreateDirectory(uploadsRoot);
                var ext = Path.GetExtension(dto.EditComprobanteFile.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                if (!allowed.Contains(ext))
                {
                    return BadRequest(new { success = false, message = "Formato de archivo no permitido. Solo JPG, PNG, PDF." });
                }
                var fileName = $"comp_{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadsRoot, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.EditComprobanteFile.CopyToAsync(stream);
                }
                var url = $"/uploads/comprobantes/{fileName}";
                var comprobante = new Data.Entities.Comprobante
                {
                    Tipo = ext == ".pdf" ? "pdf" : "img",
                    ArchivoUrl = url,
                    CreadoEn = DateTime.UtcNow
                };
                _context.Comprobantes.Add(comprobante);
                await _context.SaveChangesAsync();
                gasto.ComprobanteId = comprobante.Id;
            }

            // 6. Log de ChangeTracker
            _context.ChangeTracker.DetectChanges();
            foreach (var entry in _context.ChangeTracker.Entries())
            {
                _logger.LogInformation("Entity {Entity} State {State}", entry.Metadata.Name, entry.State);
                foreach (var prop in entry.Properties)
                {
                    _logger.LogDebug("  {Prop} = {Value}", prop.Metadata.Name, prop.CurrentValue);
                }
            }

            // 7. Guardar con manejo específico de errores
            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Gasto actualizado correctamente." });
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg)
            {
                _logger.LogError(ex, "DbUpdateException/PG: {Code} {Constraint} {Detail}", pg.SqlState, pg.ConstraintName, pg.Detail);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al guardar (DB constraint).",
                    code = pg.SqlState,
                    constraint = pg.ConstraintName,
                    detail = pg.Detail,
                    error = ex.ToString()
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException general");
                return StatusCode(500, new { success = false, message = "Error al guardar (DB).", error = ex.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción general al guardar");
                return StatusCode(500, new { success = false, message = "Error al guardar los cambios.", error = ex.ToString() });
            }
        }

        [HttpPost("agregar")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Agregar([FromForm] AddGastoDto dto)
        {
            // Log: tipo de contenido y claves recibidas
            _logger.LogInformation("[Agregar] Content-Type: {ContentType}", Request.ContentType);
            if (Request.HasFormContentType)
            {
                _logger.LogInformation("[Agregar] Form keys: {Keys}", string.Join(",", Request.Form.Keys));
                foreach (var key in Request.Form.Keys)
                {
                    StringValues val = Request.Form[key];
                    _logger.LogInformation("[Agregar] Form[{Key}] = {Value}", key, val);
                }
            }
            else
            {
                _logger.LogWarning("[Agregar] No FormContentType. Content-Type: {ContentType}", Request.ContentType);
            }

            // (0) Obtener y validar el ID del usuario autenticado
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId") ?? User.FindFirst("id");
            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var creadoPor) || creadoPor <= 0)
            {
                _logger.LogWarning("[Agregar] No se pudo determinar el usuario autenticado o el ID es inválido. Claims: {Claims}", string.Join(",", User.Claims.Select(c => $"{c.Type}={c.Value}")));
                return Unauthorized(new { success = false, message = "No se pudo determinar el usuario autenticado para creado_por." });
            }

            // (A) Validación de ModelState explícita
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(kv => kv.Value?.Errors?.Count > 0)
                    .ToDictionary(k => k.Key, v => v.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
                _logger.LogWarning("[Agregar] ModelState inválido: {@Errors}", errors);
                return BadRequest(new { success = false, message = "Datos inválidos.", modelErrors = errors });
            }

            // (B) Parseo robusto de monto y fecha (cultura invariante)
            if (!dto.TryParseMonto(out var monto))
            {
                _logger.LogWarning("[Agregar] Monto inválido: '{MontoRaw}'", dto.MontoRaw);
                return BadRequest(new { success = false, message = $"Monto inválido: '{dto.MontoRaw}' (usa punto decimal, ej. 1234.56)" });
            }

            DateOnly fecha;
            if (!string.IsNullOrWhiteSpace(dto.FechaRaw))
            {
                if (!dto.TryParseFecha(out fecha))
                {
                    _logger.LogWarning("[Agregar] Fecha inválida: '{FechaRaw}'", dto.FechaRaw);
                    return BadRequest(new { success = false, message = $"Fecha inválida: '{dto.FechaRaw}' (usa dd/MM/yyyy o yyyy-MM-dd)" });
                }
            }
            else
            {
                // Si no se envía fecha, usar la fecha de hoy en CDMX
                var mexicoTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
                var nowInMexico = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mexicoTimeZone);
                fecha = DateOnly.FromDateTime(nowInMexico);
            }

            // (C) Crear y guardar entidad respetando longitudes/nullability de DB
            var gasto = new Data.Entities.Gasto
            {
                SucursalId = dto.SucursalId!.Value,
                ConceptoGastoId = dto.ConceptoId!.Value,
                Monto = monto,
                MetodoPago = (dto.MetodoPago ?? string.Empty).Trim(),
                Descripcion = (dto.Descripcion ?? string.Empty).Trim(),
                Observaciones = (dto.Observaciones ?? string.Empty).Trim(),
                Fecha = fecha,
                Estatus = "activo",
                CreadoEn = DateTime.UtcNow,
                UpdatedEn = DateTime.UtcNow,
                CreadoPor = creadoPor
            };

            // (D) Guardar comprobante si se envió archivo
            if (dto.ComprobanteFile != null && dto.ComprobanteFile.Length > 0)
            {
                var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "comprobantes");
                if (!Directory.Exists(uploadsRoot)) Directory.CreateDirectory(uploadsRoot);
                var ext = Path.GetExtension(dto.ComprobanteFile.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                if (!allowed.Contains(ext))
                {
                    return BadRequest(new { success = false, message = "Formato de archivo no permitido. Solo JPG, PNG, PDF." });
                }
                var fileName = $"comp_{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadsRoot, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.ComprobanteFile.CopyToAsync(stream);
                }
                var url = $"/uploads/comprobantes/{fileName}";
                var comprobante = new Data.Entities.Comprobante
                {
                    Tipo = ext == ".pdf" ? "pdf" : "img",
                    ArchivoUrl = url,
                    CreadoEn = DateTime.UtcNow
                };
                _context.Comprobantes.Add(comprobante);
                await _context.SaveChangesAsync();
                gasto.ComprobanteId = comprobante.Id;
            }

            _logger.LogInformation("[Agregar] Gasto a guardar: {@Gasto}", gasto);

            try
            {
                _context.Gastos.Add(gasto);
                await _context.SaveChangesAsync();
                _logger.LogInformation("[Agregar] Gasto guardado con ID: {Id}", gasto.Id);
                return Ok(new { success = true, id = gasto.Id, message = "Gasto agregado correctamente." });
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg)
            {
                _logger.LogError(ex, "PG error {Code} {Constraint} {Detail}", pg.SqlState, pg.ConstraintName, pg.Detail);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al guardar (DB constraint).",
                    code = pg.SqlState,
                    constraint = pg.ConstraintName,
                    detail = pg.Detail,
                    error = ex.ToString()
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException");
                return StatusCode(500, new { success = false, message = "Error al guardar (DB).", error = ex.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General exception");
                return StatusCode(500, new { success = false, message = "Error al guardar los cambios.", error = ex.ToString() });
            }
        }

        [HttpDelete("eliminar/{id:long}")]
        public async Task<IActionResult> Eliminar(long id)
        {
            var gasto = await _context.Gastos.FirstOrDefaultAsync(g => g.Id == id);
            if (gasto == null)
                return NotFound(new { success = false, message = "No se encontró el gasto a eliminar." });

            _context.Gastos.Remove(gasto);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Gasto eliminado correctamente." });
        }

        [HttpGet("reporte")]
        public async Task<IActionResult> Reporte([FromQuery] long? sucursalId, [FromQuery] DateOnly? fecha)
        {
            var query = _context.Gastos
                .AsNoTracking()
                .Join(_context.Sucursals, g => g.SucursalId, s => s.Id, (g, s) => new { g, s })
                .Join(_context.ConceptosGasto, gs => gs.g.ConceptoGastoId, c => c.Id, (gs, c) => new { gs.g, gs.s, c });

            if (sucursalId.HasValue && sucursalId.Value > 0)
                query = query.Where(x => x.g.SucursalId == sucursalId.Value);
            if (fecha.HasValue)
                query = query.Where(x => x.g.Fecha == fecha.Value);

            var result = await query
                .OrderByDescending(x => x.g.Fecha)
                .ThenByDescending(x => x.g.Id)
                .Select(x => new ChikiCut.web.Pages.Operaciones.Gastos.GastoReporteVm
                {
                    Id = x.g.Id,
                    Sucursal = x.s.Name,
                    Concepto = x.c.Nombre,
                    Monto = x.g.Monto,
                    MetodoPago = x.g.MetodoPago ?? string.Empty,
                    Descripcion = x.g.Descripcion ?? string.Empty,
                    Observaciones = x.g.Observaciones ?? string.Empty,
                    Fecha = x.g.Fecha
                })
                .ToListAsync();
            return Ok(result);
        }
    }
}
