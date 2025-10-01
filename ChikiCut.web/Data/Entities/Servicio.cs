using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChikiCut.web.Data.Entities
{
    [Table("servicio", Schema = "app")]
    [Index("Codigo", Name = "ix_servicio_codigo", IsUnique = true)]
    [Index("Categoria", Name = "ix_servicio_categoria")]
    [Index("IsActive", Name = "ix_servicio_active")]
    public partial class Servicio
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("codigo")]
        [StringLength(20)]
        [Required(ErrorMessage = "El código del servicio es obligatorio.")]
        [Display(Name = "Código")]
        public string Codigo { get; set; } = string.Empty;

        [Column("nombre")]
        [StringLength(100)]
        [Required(ErrorMessage = "El nombre del servicio es obligatorio.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("descripcion")]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Column("categoria")]
        [StringLength(50)]
        [Required(ErrorMessage = "La categoría es obligatoria.")]
        [Display(Name = "Categoría")]
        public string Categoria { get; set; } = "General";

        [Column("subcategoria")]
        [StringLength(50)]
        [Display(Name = "Subcategoría")]
        public string? Subcategoria { get; set; }

        [Column("duracion_estimada")]
        [Range(1, 480, ErrorMessage = "La duración debe estar entre 1 y 480 minutos.")]
        [Display(Name = "Duración Estimada (min)")]
        public int DuracionEstimada { get; set; } = 30;

        [Column("precio_base")]
        [Precision(10, 2)]
        [Range(0, double.MaxValue, ErrorMessage = "El precio debe ser mayor o igual a cero.")]
        [Display(Name = "Precio Base")]
        public decimal PrecioBase { get; set; } = 0.00m;

        [Column("requiere_cita")]
        [Display(Name = "Requiere Cita")]
        public bool RequiereCita { get; set; } = true;

        [Column("nivel_dificultad")]
        [Range(1, 5, ErrorMessage = "El nivel de dificultad debe estar entre 1 y 5.")]
        [Display(Name = "Nivel de Dificultad")]
        public int NivelDificultad { get; set; } = 1;

        [Column("comision_empleado")]
        [Precision(5, 2)]
        [Range(0, 100, ErrorMessage = "La comisión debe estar entre 0% y 100%.")]
        [Display(Name = "Comisión Empleado (%)")]
        public decimal ComisionEmpleado { get; set; } = 10.00m;

        [Column("notas_internas")]
        [Display(Name = "Notas Internas")]
        public string? NotasInternas { get; set; }

        [Column("imagen_url")]
        [StringLength(255)]
        [Display(Name = "URL de Imagen")]
        public string? ImagenUrl { get; set; }

        [Column("tags", TypeName = "jsonb")]
        public string Tags { get; set; } = "[]";

        [Column("configuracion_adicional", TypeName = "jsonb")]
        public string ConfiguracionAdicional { get; set; } = "{}";

        [Column("is_active")]
        [Display(Name = "Servicio Activo")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("created_by")]
        public long? CreatedBy { get; set; }

        // Propiedades de navegación
        [ForeignKey("CreatedBy")]
        [InverseProperty("ServiciosCreados")]
        public virtual Usuario? UsuarioCreador { get; set; }

        [InverseProperty("Servicio")]
        public virtual ICollection<ServicioSucursal> SucursalesAsignadas { get; set; } = new List<ServicioSucursal>();

        // Propiedades calculadas
        [NotMapped]
        [Display(Name = "Estado")]
        public string EstadoDescripcion => IsActive ? "Activo" : "Inactivo";

        [NotMapped]
        [Display(Name = "Duración Formateada")]
        public string DuracionFormateada => $"{DuracionEstimada} min";

        [NotMapped]
        [Display(Name = "Precio Formateado")]
        public string PrecioFormateado => PrecioBase.ToString("C");

        [NotMapped]
        [Display(Name = "Nivel")]
        public string NivelDescripcion => NivelDificultad switch
        {
            1 => "Básico",
            2 => "Intermedio",
            3 => "Avanzado",
            4 => "Experto",
            5 => "Maestro",
            _ => "Sin definir"
        };

        [NotMapped]
        public int SucursalesAsignadasCount => SucursalesAsignadas.Count(ss => ss.IsActive);

        [NotMapped]
        public List<string> TagsList
        {
            get
            {
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<string>>(Tags) ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }
        }

        // Constantes para categorías
        public static class Categorias
        {
            public const string Cortes = "Cortes";
            public const string Peinados = "Peinados";
            public const string Barba = "Barba";
            public const string Tratamientos = "Tratamientos";
            public const string Paquetes = "Paquetes";
            public const string Especiales = "Especiales";

            public static List<string> Lista => new()
            {
                Cortes, Peinados, Barba, Tratamientos, Paquetes, Especiales
            };
        }

        // Constantes para subcategorías
        public static class Subcategorias
        {
            public const string Basicos = "Básicos";
            public const string Modernos = "Modernos";
            public const string Avanzados = "Avanzados";
            public const string Clasicos = "Clásicos";
            public const string Especializados = "Especializados";
            public const string Completos = "Completos";
            public const string Tradicionales = "Tradicionales";

            public static List<string> Lista => new()
            {
                Basicos, Modernos, Avanzados, Clasicos, Especializados, Completos, Tradicionales
            };
        }
    }
}