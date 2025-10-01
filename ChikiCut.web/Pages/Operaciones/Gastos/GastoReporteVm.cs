using System;

namespace ChikiCut.web.Pages.Operaciones.Gastos
{
    public class GastoReporteVm
    {
        public long Id { get; set; }
        public string Sucursal { get; set; } = string.Empty;
        public string Concepto { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string MetodoPago { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public DateOnly Fecha { get; set; }
    }
}
