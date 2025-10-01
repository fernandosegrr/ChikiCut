using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChikiCut.web.Data.Entities
{
    [Table("usuario_sucursal", Schema = "app")]
    [Index("UsuarioId", Name = "ix_usuario_sucursal_usuario_id")]
    [Index("SucursalId", Name = "ix_usuario_sucursal_sucursal_id")]
    [Index("IsActive", Name = "ix_usuario_sucursal_active")]
    public partial class UsuarioSucursal
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("usuario_id")]
        [Required]
        [Display(Name = "Usuario")]
        public long UsuarioId { get; set; }

        [Column("sucursal_id")]
        [Required]
        [Display(Name = "Sucursal")]
        public long SucursalId { get; set; }

        [Column("fecha_asignacion")]
        [Display(Name = "Fecha de Asignación")]
        public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;

        [Column("is_active")]
        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        [Display(Name = "Fecha de Actualización")]
        public DateTime? UpdatedAt { get; set; }

        [Column("created_by")]
        [Display(Name = "Creado Por")]
        public long? CreatedBy { get; set; }

        // Propiedades de navegación
        [ForeignKey("UsuarioId")]
        [InverseProperty("SucursalesAsignadas")]
        public virtual Usuario Usuario { get; set; } = null!;

        [ForeignKey("SucursalId")]
        [InverseProperty("UsuariosAsignados")]
        public virtual Sucursal Sucursal { get; set; } = null!;

        [ForeignKey("CreatedBy")]
        [InverseProperty("AsignacionesSucursalCreadas")]
        public virtual Usuario? UsuarioCreador { get; set; }

        // Propiedades calculadas
        [NotMapped]
        [Display(Name = "Estado")]
        public string EstadoDescripcion => IsActive ? "Activo" : "Inactivo";

        [NotMapped]
        [Display(Name = "Días Asignado")]
        public int DiasAsignado => (DateTime.UtcNow - FechaAsignacion).Days;
    }
}