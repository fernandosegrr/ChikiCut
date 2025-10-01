using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace ChikiCut.web.Api
{
    public class AddGastoDto
    {
        [FromForm(Name = "sucursalId"), Required]
        public long? SucursalId { get; set; }

        [FromForm(Name = "conceptoId"), Required]
        public long? ConceptoId { get; set; }

        [FromForm(Name = "monto"), Required]
        public string? MontoRaw { get; set; }

        [FromForm(Name = "metodoPago")]
        public string? MetodoPago { get; set; }

        [FromForm(Name = "descripcion")]
        public string? Descripcion { get; set; }

        [FromForm(Name = "observaciones")]
        public string? Observaciones { get; set; }

        [FromForm(Name = "fecha")]
        public string? FechaRaw { get; set; }

        [FromForm(Name = "ComprobanteFile")]
        public IFormFile? ComprobanteFile { get; set; }

        public bool TryParseFecha(out DateOnly fecha)
        {
            fecha = default;
            if (string.IsNullOrWhiteSpace(FechaRaw)) return false;
            return DateOnly.TryParseExact(FechaRaw, new[] { "yyyy-MM-dd", "dd/MM/yyyy" }, null, System.Globalization.DateTimeStyles.None, out fecha);
        }

        public bool TryParseMonto(out decimal monto)
        {
            monto = 0;
            if (string.IsNullOrWhiteSpace(MontoRaw)) return false;
            return decimal.TryParse(MontoRaw.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out monto);
        }
    }
}
