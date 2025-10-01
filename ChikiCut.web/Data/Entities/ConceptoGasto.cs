using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChikiCut.web.Data.Entities
{
    [Table("conceptos_gasto", Schema = "app")]
    [Index("Codigo", Name = "ix_conceptos_gasto_codigo", IsUnique = true)]
    [Index("Nombre", Name = "ix_conceptos_gasto_nombre")]
    [Index("Categoria", Name = "ix_conceptos_gasto_categoria")]
    [Index("IsActive", Name = "ix_conceptos_gasto_is_active")]
    [Index("AplicaTodasSucursales", Name = "ix_conceptos_gasto_aplica_todas_sucursales")]
    [Index("CreatedAt", Name = "ix_conceptos_gasto_created_at")]
    public partial class ConceptoGasto
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("codigo")]
        [StringLength(10)]
        [Required(ErrorMessage = "El código es obligatorio.")]
        [Display(Name = "Código")]
        public string Codigo { get; set; } = string.Empty;

        [Column("nombre")]
        [StringLength(100)]
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [Display(Name = "Nombre del Concepto")]
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

        [Column("tipo_frecuencia")]
        [StringLength(20)]
        [Display(Name = "Tipo de Frecuencia")]
        public string TipoFrecuencia { get; set; } = "Ocasional";

        [Column("requiere_comprobante")]
        [Display(Name = "Requiere Comprobante")]
        public bool RequiereComprobante { get; set; } = true;

        [Column("limite_maximo", TypeName = "decimal(10,2)")]
        [Display(Name = "Límite Máximo")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal? LimiteMaximo { get; set; }

        [Column("requiere_autorizacion")]
        [Display(Name = "Requiere Autorización")]
        public bool RequiereAutorizacion { get; set; } = false;

        [Column("nivel_autorizacion_requerido")]
        [Display(Name = "Nivel de Autorización Requerido")]
        [Range(1, 5, ErrorMessage = "El nivel debe estar entre 1 y 5.")]
        public int NivelAutorizacionRequerido { get; set; } = 1;

        [Column("cuenta_contable")]
        [StringLength(20)]
        [Display(Name = "Cuenta Contable")]
        public string? CuentaContable { get; set; }

        [Column("aplica_todas_sucursales")]
        [Display(Name = "Aplica a Todas las Sucursales")]
        public bool AplicaTodasSucursales { get; set; } = true;

        [Column("sucursales_aplicables", TypeName = "jsonb")]
        [Display(Name = "Sucursales Aplicables")]
        public string SucursalesAplicables { get; set; } = "[]";

        [Column("tags", TypeName = "jsonb")]
        [Display(Name = "Etiquetas")]
        public string Tags { get; set; } = "[]";

        [Column("configuracion_adicional", TypeName = "jsonb")]
        [Display(Name = "Configuración Adicional")]
        public string ConfiguracionAdicional { get; set; } = "{}";

        [Column("is_active")]
        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        [Display(Name = "Fecha de Modificación")]
        public DateTime? UpdatedAt { get; set; }

        [Column("created_by")]
        [Display(Name = "Creado Por")]
        public long? CreatedBy { get; set; }

        // Propiedades de navegación
        [ForeignKey("CreatedBy")]
        [InverseProperty("ConceptosGastoCreados")]
        public virtual Usuario? UsuarioCreador { get; set; }

        // Propiedades calculadas
        [NotMapped]
        [Display(Name = "Nivel")]
        public string NivelDescripcion => NivelAutorizacionRequerido switch
        {
            1 => "1 - Básico",
            2 => "2 - Intermedio", 
            3 => "3 - Supervisor",
            4 => "4 - Gerencial",
            5 => "5 - Directivo",
            _ => "Desconocido"
        };

        [NotMapped]
        [Display(Name = "Frecuencia")]
        public string FrecuenciaDescripcion => TipoFrecuencia switch
        {
            "Diario" => "Diario",
            "Semanal" => "Semanal",
            "Mensual" => "Mensual",
            "Ocasional" => "Ocasional",
            _ => TipoFrecuencia
        };

        [NotMapped]
        [Display(Name = "Estado del Límite")]
        public string EstadoLimite => LimiteMaximo.HasValue 
            ? $"Hasta {LimiteMaximo:C}" 
            : "Sin límite";

        [NotMapped]
        [Display(Name = "Autorización")]
        public string EstadoAutorizacion => RequiereAutorizacion 
            ? $"Requiere nivel {NivelAutorizacionRequerido}" 
            : "No requiere";

        [NotMapped]
        [Display(Name = "Comprobante")]
        public string EstadoComprobante => RequiereComprobante 
            ? "Obligatorio" 
            : "Opcional";

        [NotMapped]
        [Display(Name = "Alcance")]
        public string AlcanceSucursales => AplicaTodasSucursales 
            ? "Todas las sucursales" 
            : "Sucursales específicas";

        [NotMapped]
        public List<string> TagsList
        {
            get
            {
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<string>>(Tags ?? "[]") ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }
            set
            {
                Tags = System.Text.Json.JsonSerializer.Serialize(value);
            }
        }

        [NotMapped]
        public List<long> SucursalesAplicablesList
        {
            get
            {
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<long>>(SucursalesAplicables ?? "[]") ?? new List<long>();
                }
                catch
                {
                    return new List<long>();
                }
            }
            set
            {
                SucursalesAplicables = System.Text.Json.JsonSerializer.Serialize(value);
            }
        }

        [NotMapped]
        public Dictionary<string, object> ConfiguracionAdicionalDict
        {
            get
            {
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(ConfiguracionAdicional ?? "{}") ?? new Dictionary<string, object>();
                }
                catch
                {
                    return new Dictionary<string, object>();
                }
            }
            set
            {
                ConfiguracionAdicional = System.Text.Json.JsonSerializer.Serialize(value);
            }
        }

        // Constantes para categorías
        public static class Categorias
        {
            public const string Servicios = "Servicios";
            public const string Insumos = "Insumos";
            public const string Mantenimiento = "Mantenimiento";
            public const string Administrativo = "Administrativo";
            public const string Marketing = "Marketing";
            public const string RecursosHumanos = "Recursos Humanos";
            public const string Varios = "Varios";

            public static List<string> Lista => new()
            {
                Servicios, Insumos, Mantenimiento, Administrativo, 
                Marketing, RecursosHumanos, Varios
            };
        }

        // Constantes para tipos de frecuencia
        public static class TiposFrecuencia
        {
            public const string Diario = "Diario";
            public const string Semanal = "Semanal";
            public const string Mensual = "Mensual";
            public const string Ocasional = "Ocasional";

            public static List<string> Lista => new()
            {
                Diario, Semanal, Mensual, Ocasional
            };
        }
    }
}