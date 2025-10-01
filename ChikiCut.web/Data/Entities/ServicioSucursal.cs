using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data.Interfaces;

namespace ChikiCut.web.Data.Entities
{
    [Table("servicio_sucursal", Schema = "app")]
    [Index("ServicioId", Name = "ix_servicio_sucursal_servicio_id")]
    [Index("SucursalId", Name = "ix_servicio_sucursal_sucursal_id")]
    [Index("Disponible", Name = "ix_servicio_sucursal_disponible")]
    [Index("IsActive", Name = "ix_servicio_sucursal_active")]
    public partial class ServicioSucursal : ITieneSucursal
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("servicio_id")]
        [Required]
        [Display(Name = "Servicio")]
        public long ServicioId { get; set; }

        [Column("sucursal_id")]
        [Required]
        [Display(Name = "Sucursal")]
        public long SucursalId { get; set; }

        [Column("precio_local")]
        [Precision(10, 2)]
        [Range(0, double.MaxValue, ErrorMessage = "El precio debe ser mayor o igual a cero.")]
        [Required(ErrorMessage = "El precio local es obligatorio.")]
        [Display(Name = "Precio Local")]
        public decimal PrecioLocal { get; set; }

        [Column("duracion_local")]
        [Range(1, 480, ErrorMessage = "La duración debe estar entre 1 y 480 minutos.")]
        [Display(Name = "Duración Local (min)")]
        public int? DuracionLocal { get; set; }

        [Column("comision_local")]
        [Precision(5, 2)]
        [Range(0, 100, ErrorMessage = "La comisión debe estar entre 0% y 100%.")]
        [Display(Name = "Comisión Local (%)")]
        public decimal? ComisionLocal { get; set; }

        [Column("disponible")]
        [Display(Name = "Disponible")]
        public bool Disponible { get; set; } = true;

        [Column("requiere_cita_local")]
        [Display(Name = "Requiere Cita Local")]
        public bool? RequiereCitaLocal { get; set; }

        [Column("observaciones")]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }

        [Column("descuento_maximo")]
        [Precision(5, 2)]
        [Range(0, 100, ErrorMessage = "El descuento máximo debe estar entre 0% y 100%.")]
        [Display(Name = "Descuento Máximo (%)")]
        public decimal DescuentoMaximo { get; set; } = 0.00m;

        [Column("orden_display")]
        [Display(Name = "Orden de Visualización")]
        public int OrdenDisplay { get; set; } = 0;

        [Column("fecha_inicio_disponibilidad")]
        [Display(Name = "Fecha Inicio Disponibilidad")]
        public DateOnly? FechaInicioDisponibilidad { get; set; }

        [Column("fecha_fin_disponibilidad")]
        [Display(Name = "Fecha Fin Disponibilidad")]
        public DateOnly? FechaFinDisponibilidad { get; set; }

        [Column("configuracion_local", TypeName = "jsonb")]
        public string ConfiguracionLocal { get; set; } = "{}";

        [Column("is_active")]
        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("created_by")]
        public long? CreatedBy { get; set; }

        // Propiedades de navegación
        [ForeignKey("ServicioId")]
        [InverseProperty("SucursalesAsignadas")]
        public virtual Servicio Servicio { get; set; } = null!;

        [ForeignKey("SucursalId")]
        [InverseProperty("ServiciosAsignados")]
        public virtual Sucursal Sucursal { get; set; } = null!;

        [ForeignKey("CreatedBy")]
        [InverseProperty("ServicioSucursalesCreados")]
        public virtual Usuario? UsuarioCreador { get; set; }

        // Propiedades calculadas
        [NotMapped]
        [Display(Name = "Estado")]
        public string EstadoDescripcion => 
            !IsActive ? "Inactivo" 
            : !Disponible ? "No Disponible" 
            : "Disponible";

        [NotMapped]
        [Display(Name = "Duración Efectiva")]
        public int DuracionEfectiva => DuracionLocal ?? Servicio?.DuracionEstimada ?? 30;

        [NotMapped]
        [Display(Name = "Comisión Efectiva")]
        public decimal ComisionEfectiva => ComisionLocal ?? Servicio?.ComisionEmpleado ?? 10.00m;

        [NotMapped]
        [Display(Name = "Requiere Cita Efectivo")]
        public bool RequiereCitaEfectivo => RequiereCitaLocal ?? Servicio?.RequiereCita ?? true;

        [NotMapped]
        [Display(Name = "Precio Formateado")]
        public string PrecioFormateado => PrecioLocal.ToString("C");

        [NotMapped]
        [Display(Name = "Duración Formateada")]
        public string DuracionFormateada => $"{DuracionEfectiva} min";

        [NotMapped]
        public bool EstaDisponibleHoy
        {
            get
            {
                var hoy = DateOnly.FromDateTime(DateTime.Today);
                return Disponible && IsActive &&
                       (FechaInicioDisponibilidad == null || hoy >= FechaInicioDisponibilidad) &&
                       (FechaFinDisponibilidad == null || hoy <= FechaFinDisponibilidad);
            }
        }

        [NotMapped]
        public Dictionary<string, object> ConfiguracionLocalDict
        {
            get
            {
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(ConfiguracionLocal) 
                           ?? new Dictionary<string, object>();
                }
                catch
                {
                    return new Dictionary<string, object>();
                }
            }
        }

        // Método para calcular precio con descuento
        public decimal CalcularPrecioConDescuento(decimal porcentajeDescuento)
        {
            if (porcentajeDescuento < 0) porcentajeDescuento = 0;
            if (porcentajeDescuento > DescuentoMaximo) porcentajeDescuento = DescuentoMaximo;

            return PrecioLocal * (1 - porcentajeDescuento / 100);
        }

        // Método para verificar si puede aplicarse un descuento
        public bool PuedeAplicarDescuento(decimal porcentajeDescuento)
        {
            return porcentajeDescuento >= 0 && porcentajeDescuento <= DescuentoMaximo;
        }
    }
}