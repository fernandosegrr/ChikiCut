using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ChikiCut.web.Extensions
{
    public static class DbContextExtensions
    {
        /// <summary>
        /// Convierte todos los DateTime sin especificar Kind a UTC antes de guardar
        /// </summary>
        public static void EnsureUtcDateTimes(this DbContext context)
        {
            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    foreach (var property in entry.Properties)
                    {
                        if (property.CurrentValue is DateTime dateTime)
                        {
                            if (dateTime.Kind == DateTimeKind.Unspecified)
                            {
                                property.CurrentValue = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                            }
                            else if (dateTime.Kind == DateTimeKind.Local)
                            {
                                property.CurrentValue = dateTime.ToUniversalTime();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Guarda los cambios asegurando que todos los DateTime sean UTC
        /// </summary>
        public static async Task<int> SaveChangesUtcAsync(this DbContext context, CancellationToken cancellationToken = default)
        {
            context.EnsureUtcDateTimes();
            return await context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Guarda los cambios asegurando que todos los DateTime sean UTC (versión síncrona)
        /// </summary>
        public static int SaveChangesUtc(this DbContext context)
        {
            context.EnsureUtcDateTimes();
            return context.SaveChanges();
        }
    }
}