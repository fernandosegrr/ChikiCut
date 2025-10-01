using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Data.Interfaces;
using ChikiCut.web.Extensions;

namespace ChikiCut.web.Services
{
    /// <summary>
    /// Implementación del servicio de filtrado por sucursal
    /// </summary>
    public class SucursalFilterService : ISucursalFilterService
    {
        private readonly AppDbContext _context;

        public SucursalFilterService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todas las sucursales a las que el usuario tiene acceso
        /// Combina: sucursales asignadas + sucursal del empleado
        /// </summary>
        public async Task<List<long>> GetSucursalesUsuarioAsync(long usuarioId)
        {
            var sucursales = new List<long>();

            // 1. Obtener sucursales asignadas específicamente
            var sucursalesAsignadas = await _context.UsuarioSucursales
                .Where(us => us.UsuarioId == usuarioId && us.IsActive)
                .Select(us => us.SucursalId)
                .ToListAsync();

            sucursales.AddRange(sucursalesAsignadas);

            // 2. Si no tiene asignaciones específicas, usar la sucursal del empleado
            if (!sucursales.Any())
            {
                var sucursalEmpleado = await GetSucursalEmpleadoAsync(usuarioId);
                if (sucursalEmpleado.HasValue)
                {
                    sucursales.Add(sucursalEmpleado.Value);
                }
            }

            return sucursales.Distinct().ToList();
        }

        /// <summary>
        /// Filtra una consulta para mostrar solo datos de las sucursales del usuario
        /// </summary>
        public async Task<IQueryable<T>> FiltrarPorSucursalAsync<T>(IQueryable<T> query, long usuarioId) 
            where T : class, ITieneSucursal
        {
            var sucursalesUsuario = await GetSucursalesUsuarioAsync(usuarioId);
            
            if (!sucursalesUsuario.Any())
            {
                // Si no tiene sucursales asignadas, no mostrar nada
                return query.Where(x => false);
            }

            return query.Where(entity => sucursalesUsuario.Contains(entity.SucursalId));
        }

        /// <summary>
        /// Verifica si el usuario tiene acceso a una sucursal específica
        /// </summary>
        public async Task<bool> UsuarioTieneAccesoSucursalAsync(long usuarioId, long sucursalId)
        {
            var sucursalesUsuario = await GetSucursalesUsuarioAsync(usuarioId);
            return sucursalesUsuario.Contains(sucursalId);
        }

        /// <summary>
        /// Asigna múltiples sucursales a un usuario, reemplazando las existentes
        /// </summary>
        public async Task AsignarSucursalesUsuarioAsync(long usuarioId, List<long> sucursalesIds, long? createdBy = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // 1. Desactivar todas las asignaciones actuales
                var asignacionesActuales = await _context.UsuarioSucursales
                    .Where(us => us.UsuarioId == usuarioId)
                    .ToListAsync();

                foreach (var asignacion in asignacionesActuales)
                {
                    asignacion.IsActive = false;
                    asignacion.UpdatedAt = DateTime.UtcNow;
                }

                // 2. Crear nuevas asignaciones
                foreach (var sucursalId in sucursalesIds.Distinct())
                {
                    // Verificar si ya existe esta asignación
                    var asignacionExistente = asignacionesActuales
                        .FirstOrDefault(a => a.SucursalId == sucursalId);

                    if (asignacionExistente != null)
                    {
                        // Reactivar la existente
                        asignacionExistente.IsActive = true;
                        asignacionExistente.FechaAsignacion = DateTime.UtcNow;
                        asignacionExistente.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        // Crear nueva asignación
                        var nuevaAsignacion = new UsuarioSucursal
                        {
                            UsuarioId = usuarioId,
                            SucursalId = sucursalId,
                            FechaAsignacion = DateTime.UtcNow,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = createdBy
                        };

                        _context.UsuarioSucursales.Add(nuevaAsignacion);
                    }
                }

                await _context.SaveChangesUtcAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Desasigna una sucursal específica de un usuario
        /// </summary>
        public async Task DesasignarSucursalUsuarioAsync(long usuarioId, long sucursalId)
        {
            var asignacion = await _context.UsuarioSucursales
                .FirstOrDefaultAsync(us => us.UsuarioId == usuarioId && 
                                          us.SucursalId == sucursalId && 
                                          us.IsActive);

            if (asignacion != null)
            {
                asignacion.IsActive = false;
                asignacion.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesUtcAsync();
            }
        }

        /// <summary>
        /// Verifica si el usuario tiene acceso global (todas las sucursales activas)
        /// </summary>
        public async Task<bool> TieneAccesoGlobalAsync(long usuarioId)
        {
            var sucursalesUsuario = await GetSucursalesUsuarioAsync(usuarioId);
            var totalSucursalesActivas = await _context.Sucursals
                .CountAsync(s => s.IsActive);

            return sucursalesUsuario.Count >= totalSucursalesActivas;
        }

        /// <summary>
        /// Obtiene la sucursal del empleado asociado al usuario
        /// </summary>
        public async Task<long?> GetSucursalEmpleadoAsync(long usuarioId)
        {
            return await _context.Usuarios
                .Where(u => u.Id == usuarioId)
                .Select(u => u.Empleado.SucursalId)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Sincroniza las asignaciones: si no tiene asignaciones, asigna la sucursal del empleado
        /// </summary>
        public async Task SincronizarSucursalesUsuarioAsync(long usuarioId)
        {
            var tieneAsignaciones = await _context.UsuarioSucursales
                .AnyAsync(us => us.UsuarioId == usuarioId && us.IsActive);

            if (!tieneAsignaciones)
            {
                var sucursalEmpleado = await GetSucursalEmpleadoAsync(usuarioId);
                if (sucursalEmpleado.HasValue)
                {
                    await AsignarSucursalesUsuarioAsync(usuarioId, [sucursalEmpleado.Value]);
                }
            }
        }
    }
}