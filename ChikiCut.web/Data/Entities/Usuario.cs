using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChikiCut.web.Data.Entities
{
    [Table("usuario", Schema = "app")]
    [Index("Email", Name = "ix_usuario_email", IsUnique = true)]
    [Index("CodigoUsuario", Name = "ix_usuario_codigo", IsUnique = true)]
    [Index("IsActive", Name = "ix_usuario_is_active")]
    public partial class Usuario
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("empleado_id")]
        [Required(ErrorMessage = "Debe seleccionar un empleado.")]
        [Display(Name = "Empleado")]
        public long EmpleadoId { get; set; }

        [Column("codigo_usuario")]
        [StringLength(20)]
        [Required(ErrorMessage = "El código de usuario es obligatorio.")]
        [Display(Name = "Código de Usuario")]
        public string CodigoUsuario { get; set; } = string.Empty;

        [Column("email")]
        [StringLength(120)]
        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "Debe ser un email válido.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Column("password_hash")]
        [StringLength(255)]
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("rol_id")]
        [Required(ErrorMessage = "Debe seleccionar un rol.")]
        [Display(Name = "Rol")]
        public long RolId { get; set; }

        [Column("ultimo_acceso")]
        [Display(Name = "Último Acceso")]
        public DateTime? UltimoAcceso { get; set; }

        [Column("intentos_fallidos")]
        public int IntentosFallidos { get; set; } = 0;

        [Column("bloqueado_hasta")]
        public DateTime? BloqueadoHasta { get; set; }

        [Column("is_active")]
        [Display(Name = "Usuario Activo")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("created_by")]
        public long? CreatedBy { get; set; }

        // Propiedades de navegación
        [ForeignKey("EmpleadoId")]
        [InverseProperty("Usuario")]
        public virtual Empleado Empleado { get; set; } = null!;

        [ForeignKey("RolId")]
        [InverseProperty("Usuarios")]
        public virtual Rol Rol { get; set; } = null!;

        [ForeignKey("CreatedBy")]
        [InverseProperty("UsuariosCreados")]
        public virtual Usuario? UsuarioCreador { get; set; }

        [InverseProperty("UsuarioCreador")]
        public virtual ICollection<Usuario> UsuariosCreados { get; set; } = new List<Usuario>();

        [InverseProperty("UsuarioCreador")]
        public virtual ICollection<ConceptoGasto> ConceptosGastoCreados { get; set; } = new List<ConceptoGasto>();

        // NUEVA NAVEGACIÓN PARA SUCURSALES
        [InverseProperty("Usuario")]
        public virtual ICollection<UsuarioSucursal> SucursalesAsignadas { get; set; } = new List<UsuarioSucursal>();

        [InverseProperty("UsuarioCreador")]
        public virtual ICollection<UsuarioSucursal> AsignacionesSucursalCreadas { get; set; } = new List<UsuarioSucursal>();

        // NUEVA NAVEGACIÓN PARA SERVICIOS
        [InverseProperty("UsuarioCreador")]
        public virtual ICollection<Servicio> ServiciosCreados { get; set; } = new List<Servicio>();

        [InverseProperty("UsuarioCreador")]
        public virtual ICollection<ServicioSucursal> ServicioSucursalesCreados { get; set; } = new List<ServicioSucursal>();

        // NUEVA NAVEGACIÓN PARA PRODUCTOS
        [InverseProperty("UsuarioCreador")]
        public virtual ICollection<Producto> ProductosCreados { get; set; } = new List<Producto>();

        [InverseProperty("UsuarioCreador")]
        public virtual ICollection<ProductoSucursal> ProductoSucursalesCreados { get; set; } = new List<ProductoSucursal>();

        // Propiedades calculadas
        [NotMapped]
        [Display(Name = "Nombre Completo")]
        public string NombreCompleto => Empleado?.NombreCompleto ?? "Sin empleado";

        // NUEVA PROPIEDAD CALCULADA PARA SUCURSALES
        [NotMapped]
        public List<long> SucursalesIds => SucursalesAsignadas
            .Where(us => us.IsActive)
            .Select(us => us.SucursalId)
            .ToList();

        [NotMapped]
        public List<Sucursal> SucursalesActivas => SucursalesAsignadas
            .Where(us => us.IsActive)
            .Select(us => us.Sucursal)
            .Where(s => s.IsActive)
            .ToList();

        [NotMapped]
        public string SucursalesNombres => string.Join(", ", 
            SucursalesActivas.Select(s => s.Name).OrderBy(name => name));

        [NotMapped]
        [Display(Name = "Estado")]
        public string EstadoDescripcion => 
            !IsActive ? "Inactivo" 
            : BloqueadoHasta.HasValue && BloqueadoHasta > DateTime.UtcNow ? "Bloqueado" 
            : "Activo";

        [NotMapped]
        public bool EstaBloqueado => BloqueadoHasta.HasValue && BloqueadoHasta > DateTime.UtcNow;

        [NotMapped]
        public bool PuedeAcceder => IsActive && !EstaBloqueado;

        [NotMapped]
        public string SucursalNombre => Empleado?.Sucursal?.Name ?? "Sin sucursal";
    }
}