using ChikiCut.web.Data.Entities;
using ChikiCut.web.Helpers;

namespace ChikiCut.web.Extensions
{
    public static class SucursalExtensions
    {
        /// <summary>
        /// Formatea los horarios de apertura en formato completo (una línea por día)
        /// </summary>
        public static string GetFormattedOpeningHours(this Sucursal sucursal)
        {
            return OpeningHoursHelper.FormatOpeningHours(sucursal.OpeningHours);
        }

        /// <summary>
        /// Formatea los horarios de apertura en formato compacto (agrupando días similares)
        /// </summary>
        public static string GetCompactOpeningHours(this Sucursal sucursal)
        {
            return OpeningHoursHelper.FormatOpeningHoursCompact(sucursal.OpeningHours);
        }
    }
}