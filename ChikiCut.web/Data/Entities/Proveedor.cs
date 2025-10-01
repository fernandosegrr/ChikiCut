using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ChikiCut.web.Data.Entities
{
    [Table("proveedor", Schema = "app")]
    [Index("NombreComercial", Name = "ix_proveedor_nombre")]
    [Index("Rfc", Name = "ix_proveedor_rfc")]
    [Index("CodigoProveedor", Name = "ix_proveedor_codigo", IsUnique = true)]
    [Index("IsActive", Name = "ix_proveedor_activo")]
    [Index("Categoria", Name = "ix_proveedor_categoria")]
    public partial class Proveedor
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        // Informaci�n b�sica
        [Column("nombre_comercial")]
        [StringLength(150)]
        [Required(ErrorMessage = "El nombre comercial es obligatorio.")]
        [Display(Name = "Nombre Comercial")]
        public string NombreComercial { get; set; } = string.Empty;

        [Column("razon_social")]
        [StringLength(200)]
        [Display(Name = "Raz�n Social")]
        public string? RazonSocial { get; set; }

        [Column("rfc")]
        [StringLength(13)]
        [Display(Name = "RFC")]
        [RegularExpression(@"^[A-Z&�]{3,4}[0-9]{6}[A-Z0-9]{3}$", ErrorMessage = "El RFC no tiene un formato v�lido.")]
        public string? Rfc { get; set; }

        [Column("codigo_proveedor")]
        [StringLength(20)]
        [Display(Name = "C�digo de Proveedor")]
        public string? CodigoProveedor { get; set; }

        // Informaci�n de contacto
        [Column("contacto_principal")]
        [StringLength(100)]
        [Display(Name = "Contacto Principal")]
        public string? ContactoPrincipal { get; set; }

        [Column("telefono_principal")]
        [StringLength(20)]
        [Display(Name = "Tel�fono Principal")]
        [Phone(ErrorMessage = "El formato del tel�fono no es v�lido.")]
        public string? TelefonoPrincipal { get; set; }

        [Column("telefono_alternativo")]
        [StringLength(20)]
        [Display(Name = "Tel�fono Alternativo")]
        [Phone(ErrorMessage = "El formato del tel�fono no es v�lido.")]
        public string? TelefonoAlternativo { get; set; }

        [Column("email")]
        [StringLength(100)]
        [EmailAddress(ErrorMessage = "El formato del email no es v�lido.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Column("sitio_web")]
        [StringLength(200)]
        [Url(ErrorMessage = "El formato de la URL no es v�lido.")]
        [Display(Name = "Sitio Web")]
        public string? SitioWeb { get; set; }

        // Direcci�n
        [Column("direccion_linea1")]
        [StringLength(200)]
        [Display(Name = "Direcci�n")]
        public string? DireccionLinea1 { get; set; }

        [Column("direccion_linea2")]
        [StringLength(200)]
        [Display(Name = "Direcci�n L�nea 2")]
        public string? DireccionLinea2 { get; set; }

        [Column("ciudad")]
        [StringLength(100)]
        [Display(Name = "Ciudad")]
        public string? Ciudad { get; set; }

        [Column("estado")]
        [StringLength(100)]
        [Display(Name = "Estado")]
        public string? Estado { get; set; }

        [Column("codigo_postal")]
        [StringLength(10)]
        [Display(Name = "C�digo Postal")]
        public string? CodigoPostal { get; set; }

        [Column("pais")]
        [StringLength(2)]
        [Display(Name = "Pa�s")]
        public string Pais { get; set; } = "MX";

        // Informaci�n comercial
        [Column("tipos_productos", TypeName = "jsonb")]
        [Display(Name = "Tipos de Productos")]
        public string TiposProductos { get; set; } = "[]";

        [Column("condiciones_pago")]
        [StringLength(100)]
        [Display(Name = "Condiciones de Pago")]
        public string CondicionesPago { get; set; } = "Contado";

        [Column("dias_credito")]
        [Display(Name = "D�as de Cr�dito")]
        [Range(0, 180, ErrorMessage = "Los d�as de cr�dito deben estar entre 0 y 180.")]
        public int DiasCredito { get; set; } = 0;

        [Column("descuento_pronto_pago")]
        [Display(Name = "Descuento Pronto Pago (%)")]
        [Range(0, 100, ErrorMessage = "El descuento debe estar entre 0 y 100%.")]
        public decimal DescuentoProntoPago { get; set; } = 0.00m;

        [Column("moneda")]
        [StringLength(3)]
        [Display(Name = "Moneda")]
        public string Moneda { get; set; } = "MXN";

        // Informaci�n bancaria
        [Column("banco")]
        [StringLength(100)]
        [Display(Name = "Banco")]
        public string? Banco { get; set; }

        [Column("numero_cuenta")]
        [StringLength(30)]
        [Display(Name = "N�mero de Cuenta")]
        public string? NumeroCuenta { get; set; }

        [Column("clabe")]
        [StringLength(18)]
        [Display(Name = "CLABE")]
        [RegularExpression(@"^[0-9]{18}$", ErrorMessage = "La CLABE debe tener exactamente 18 d�gitos.")]
        public string? Clabe { get; set; }

        // Clasificaci�n y notas
        [Column("categoria")]
        [StringLength(50)]
        [Display(Name = "Categor�a")]
        public string Categoria { get; set; } = "General";

        [Column("calificacion")]
        [Display(Name = "Calificaci�n")]
        [Range(1, 5, ErrorMessage = "La calificaci�n debe estar entre 1 y 5.")]
        public int? Calificacion { get; set; }

        [Column("notas")]
        [Display(Name = "Notas")]
        public string? Notas { get; set; }

        // Campos de sistema
        [Column("is_active")]
        [Display(Name = "Proveedor Activo")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        [Display(Name = "Fecha de Creaci�n")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        [Display(Name = "Fecha de Actualizaci�n")]
        public DateTime? UpdatedAt { get; set; }

        [Column("created_by")]
        public long? CreatedBy { get; set; }

        [Column("updated_by")]
        public long? UpdatedBy { get; set; }

        [InverseProperty("ProveedorPrincipal")]
        public virtual ICollection<Producto> ProductosProveidos { get; set; } = new List<Producto>();

        // Propiedades calculadas
        [NotMapped]
        [Display(Name = "Estado")]
        public string EstadoDescripcion => IsActive ? "Activo" : "Inactivo";

        [NotMapped]
        [Display(Name = "Nombre Completo")]
        public string NombreCompleto => !string.IsNullOrEmpty(RazonSocial) && RazonSocial != NombreComercial 
            ? $"{NombreComercial} ({RazonSocial})" 
            : NombreComercial;

        [NotMapped]
        [Display(Name = "Direcci�n Completa")]
        public string DireccionCompleta
        {
            get
            {
                var partes = new List<string>();
                
                if (!string.IsNullOrEmpty(DireccionLinea1))
                    partes.Add(DireccionLinea1);
                
                if (!string.IsNullOrEmpty(Ciudad))
                    partes.Add(Ciudad);
                
                if (!string.IsNullOrEmpty(Estado))
                    partes.Add(Estado);
                
                if (!string.IsNullOrEmpty(CodigoPostal))
                    partes.Add($"CP {CodigoPostal}");
                
                return string.Join(", ", partes);
            }
        }

        [NotMapped]
        [Display(Name = "Lista de Productos")]
        public List<string> ListaTiposProductos
        {
            get
            {
                try
                {
                    return JsonSerializer.Deserialize<List<string>>(TiposProductos ?? "[]") ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }
            set
            {
                TiposProductos = JsonSerializer.Serialize(value ?? new List<string>());
            }
        }

        [NotMapped]
        [Display(Name = "Productos Resumidos")]
        public string ProductosResumen
        {
            get
            {
                var productos = ListaTiposProductos;
                if (!productos.Any())
                    return "Sin productos configurados";
                
                if (productos.Count <= 3)
                    return string.Join(", ", productos);
                
                return $"{string.Join(", ", productos.Take(2))} y {productos.Count - 2} m�s";
            }
        }

        [NotMapped]
        [Display(Name = "Calificaci�n con Estrellas")]
        public string CalificacionEstrellas
        {
            get
            {
                if (!Calificacion.HasValue) return "Sin calificar";
                
                var estrellas = string.Concat(Enumerable.Repeat("?", Calificacion.Value));
                var vacias = string.Concat(Enumerable.Repeat("?", 5 - Calificacion.Value));
                return estrellas + vacias;
            }
        }

        [NotMapped]
        [Display(Name = "Estado de Cr�dito")]
        public string EstadoCredito => DiasCredito > 0 ? $"Cr�dito {DiasCredito} d�as" : "Contado";

        [NotMapped]
        [Display(Name = "Informaci�n de Contacto")]
        public string ContactoCompleto
        {
            get
            {
                var partes = new List<string>();
                
                if (!string.IsNullOrEmpty(ContactoPrincipal))
                    partes.Add(ContactoPrincipal);
                
                if (!string.IsNullOrEmpty(TelefonoPrincipal))
                    partes.Add($"Tel: {TelefonoPrincipal}");
                
                if (!string.IsNullOrEmpty(Email))
                    partes.Add($"Email: {Email}");
                
                return string.Join(" | ", partes);
            }
        }
    }
}