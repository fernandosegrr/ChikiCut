namespace ChikiCut.web.Data.Interfaces
{
    /// <summary>
    /// Interfaz que deben implementar las entidades que pertenecen a una sucursal específica
    /// </summary>
    public interface ITieneSucursal
    {
        /// <summary>
        /// ID de la sucursal a la que pertenece esta entidad
        /// </summary>
        long SucursalId { get; }
    }
}