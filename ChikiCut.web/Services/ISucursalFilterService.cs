using ChikiCut.web.Data.Interfaces;

namespace ChikiCut.web.Services
{
    /// <summary>
    /// Servicio para filtrar datos por sucursal según los permisos del usuario
    /// </summary>
    public interface ISucursalFilterService
    {
        /// <summary>
        /// Obtiene la lista de IDs de sucursales a las que el usuario tiene acceso
        /// </summary>
        /// <param name="usuarioId">ID del usuario</param>
        /// <returns>Lista de IDs de sucursales</returns>
        Task<List<long>> GetSucursalesUsuarioAsync(long usuarioId);

        /// <summary>
        /// Filtra una consulta para mostrar solo datos de las sucursales del usuario
        /// </summary>
        /// <typeparam name="T">Tipo de entidad que implementa ITieneSucursal</typeparam>
        /// <param name="query">Consulta a filtrar</param>
        /// <param name="usuarioId">ID del usuario</param>
        /// <returns>Consulta filtrada por sucursales</returns>
        Task<IQueryable<T>> FiltrarPorSucursalAsync<T>(IQueryable<T> query, long usuarioId) 
            where T : class, ITieneSucursal;

        /// <summary>
        /// Verifica si el usuario tiene acceso a una sucursal específica
        /// </summary>
        /// <param name="usuarioId">ID del usuario</param>
        /// <param name="sucursalId">ID de la sucursal</param>
        /// <returns>True si tiene acceso, False si no</returns>
        Task<bool> UsuarioTieneAccesoSucursalAsync(long usuarioId, long sucursalId);

        /// <summary>
        /// Asigna múltiples sucursales a un usuario
        /// </summary>
        /// <param name="usuarioId">ID del usuario</param>
        /// <param name="sucursalesIds">Lista de IDs de sucursales a asignar</param>
        /// <param name="createdBy">ID del usuario que realiza la asignación</param>
        Task AsignarSucursalesUsuarioAsync(long usuarioId, List<long> sucursalesIds, long? createdBy = null);

        /// <summary>
        /// Desasigna una sucursal específica de un usuario
        /// </summary>
        /// <param name="usuarioId">ID del usuario</param>
        /// <param name="sucursalId">ID de la sucursal a desasignar</param>
        Task DesasignarSucursalUsuarioAsync(long usuarioId, long sucursalId);

        /// <summary>
        /// Verifica si el usuario tiene acceso global a todas las sucursales
        /// </summary>
        /// <param name="usuarioId">ID del usuario</param>
        /// <returns>True si tiene acceso global</returns>
        Task<bool> TieneAccesoGlobalAsync(long usuarioId);

        /// <summary>
        /// Obtiene la sucursal principal del usuario (la de su empleado)
        /// </summary>
        /// <param name="usuarioId">ID del usuario</param>
        /// <returns>ID de la sucursal del empleado</returns>
        Task<long?> GetSucursalEmpleadoAsync(long usuarioId);

        /// <summary>
        /// Sincroniza las asignaciones: asigna la sucursal del empleado si no tiene asignaciones
        /// </summary>
        /// <param name="usuarioId">ID del usuario</param>
        Task SincronizarSucursalesUsuarioAsync(long usuarioId);
    }
}