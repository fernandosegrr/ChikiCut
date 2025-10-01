using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Microsoft.AspNetCore.Http;

public sealed class EditGastoDto
{
    [FromForm(Name = "editGastoId"), Required]
    public long? GastoId { get; set; }
    [FromForm(Name = "editSucursalId"), Required]
    public long? SucursalId { get; set; }
    [FromForm(Name = "editConceptoId"), Required]
    public long? ConceptoId { get; set; }

    [FromForm(Name = "editMonto"), Required]
    public string? MontoRaw { get; set; }

    [FromForm(Name = "editMetodoPago")]
    public string? MetodoPago { get; set; }
    [FromForm(Name = "editDescripcion")]
    public string? Descripcion { get; set; }
    [FromForm(Name = "editObservaciones")]
    public string? Observaciones { get; set; }
    [FromForm(Name = "editFecha")]
    public string? FechaRaw { get; set; }

    [FromForm(Name = "editComprobanteFile")]
    public IFormFile? EditComprobanteFile { get; set; }

    public bool TryParseFecha(out DateOnly fecha)
    {
        var formats = new[] { "dd/MM/yyyy", "yyyy-MM-dd" };
        return DateOnly.TryParseExact(FechaRaw, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);
    }

    public bool TryParseMonto(out decimal monto)
    {
        var raw = (MontoRaw ?? "").Trim().Replace(',', '.');
        return decimal.TryParse(raw, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out monto);
    }
}
