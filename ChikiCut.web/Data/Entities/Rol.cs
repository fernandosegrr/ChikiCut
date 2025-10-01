using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChikiCut.web.Data.Entities
{
    [Table("rol", Schema = "app")]
    [Index("Nombre", Name = "ix_rol_nombre", IsUnique = true)]
    [Index("IsActive", Name = "ix_rol_is_active")]
    [Index("NivelAcceso", Name = "ix_rol_nivel_acceso")]
    public partial class Rol
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("nombre")]
        [StringLength(50)]
        [Required(ErrorMessage = "El nombre del rol es obligatorio.")]
        [Display(Name = "Nombre del Rol")]
        public string Nombre { get; set; } = string.Empty;

        [Column("descripcion")]
        [StringLength(200)]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Column("nivel_acceso")]
        [Display(Name = "Nivel de Acceso")]
        public int NivelAcceso { get; set; } = 1;

        [Column("permisos", TypeName = "jsonb")]
        [Display(Name = "Permisos")]
        public string Permisos { get; set; } = "{}";

        [Column("is_active")]
        [Display(Name = "Rol Activo")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Propiedades de navegación
        [InverseProperty("Rol")]
        public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();

        // Propiedades calculadas
        [NotMapped]
        [Display(Name = "Nivel")]
        public string NivelDescripcion => NivelAcceso switch
        {
            1 => "1 - Limitado",
            2 => "2 - Básico", 
            3 => "3 - Intermedio",
            4 => "4 - Avanzado",
            5 => "5 - Administrador",
            _ => "Desconocido"
        };

        [NotMapped]
        public int CantidadUsuarios => Usuarios?.Count(u => u.IsActive) ?? 0;

        [NotMapped]
        public string PermisosResumen
        {
            get
            {
                try
                {
                    var permisos = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(Permisos ?? "{}");
                    return $"{permisos?.Count ?? 0} módulos configurados";
                }
                catch
                {
                    return "Configuración inválida";
                }
            }
        }
    }
}