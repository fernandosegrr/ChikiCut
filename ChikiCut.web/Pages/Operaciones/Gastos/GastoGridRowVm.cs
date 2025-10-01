namespace ChikiCut.web.Pages.Operaciones.Gastos
{
    public record GastoGridRowVm(
        long Id,
        string SucursalNombre,
        string ConceptoNombre,
        decimal Monto,
        string MetodoPago,
        string Descripcion,
        string Observaciones,
        DateOnly Fecha,
        string? ComprobanteUrl // Nueva propiedad para la URL del comprobante
    );
}
