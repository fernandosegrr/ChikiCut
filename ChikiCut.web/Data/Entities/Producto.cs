using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChikiCut.web.Data.Entities
{
    [Table("producto", Schema = "app")]
    [Index("Codigo", Name = "ix_producto_codigo", IsUnique = true)]
    [Index("Nombre", Name = "ix_producto_nombre")]
    [Index("Marca", Name = "ix_producto_marca")]
    [Index("Categoria", Name = "ix_producto_categoria")]
    [Index("IsActive", Name = "ix_producto_active")]
    public partial class Producto
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("codigo")]
        [StringLength(30)]
        [Required(ErrorMessage = "El código del producto es obligatorio.")]
        [Display(Name = "Código")]
        public string Codigo { get; set; } = string.Empty;

        [Column("nombre")]
        [StringLength(150)]
        [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("descripcion")]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Column("marca")]
        [StringLength(80)]
        [Required(ErrorMessage = "La marca es obligatoria.")]
        [Display(Name = "Marca")]
        public string Marca { get; set; } = string.Empty;

        [Column("categoria")]
        [StringLength(60)]
        [Required(ErrorMessage = "La categoría es obligatoria.")]
        [Display(Name = "Categoría")]
        public string Categoria { get; set; } = "General";

        [Column("subcategoria")]
        [StringLength(60)]
        [Display(Name = "Subcategoría")]
        public string? Subcategoria { get; set; }

        [Column("tipo_producto")]
        [StringLength(30)]
        [Display(Name = "Tipo de Producto")]
        public string TipoProducto { get; set; } = "Físico";

        [Column("unidad_medida")]
        [StringLength(20)]
        [Display(Name = "Unidad de Medida")]
        public string UnidadMedida { get; set; } = "Pieza";

        [Column("contenido_neto")]
        [Precision(10, 3)]
        [Display(Name = "Contenido Neto")]
        public decimal? ContenidoNeto { get; set; }

        [Column("precio_base")]
        [Precision(10, 2)]
        [Range(0, double.MaxValue, ErrorMessage = "El precio debe ser mayor o igual a cero.")]
        [Required(ErrorMessage = "El precio base es obligatorio.")]
        [Display(Name = "Precio Base")]
        public decimal PrecioBase { get; set; } = 0.00m;

        [Column("costo_promedio")]
        [Precision(10, 2)]
        [Range(0, double.MaxValue, ErrorMessage = "El costo debe ser mayor o igual a cero.")]
        [Display(Name = "Costo Promedio")]
        public decimal? CostoPromedio { get; set; }

        [Column("margen_ganancia_sugerido")]
        [Precision(5, 2)]
        [Range(0, 100, ErrorMessage = "El margen debe estar entre 0% y 100%.")]
        [Display(Name = "Margen de Ganancia (%)")]
        public decimal MargenGananciaSugerido { get; set; } = 30.00m;

        [Column("sku")]
        [StringLength(50)]
        [Display(Name = "SKU")]
        public string? Sku { get; set; }

        [Column("codigo_barras")]
        [StringLength(20)]
        [Display(Name = "Código de Barras")]
        public string? CodigoBarras { get; set; }

        [Column("requiere_inventario")]
        [Display(Name = "Requiere Inventario")]
        public bool RequiereInventario { get; set; } = true;

        [Column("stock_minimo")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo debe ser mayor o igual a cero.")]
        [Display(Name = "Stock Mínimo")]
        public int StockMinimo { get; set; } = 0;

        [Column("stock_maximo")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock máximo debe ser mayor o igual a cero.")]
        [Display(Name = "Stock Máximo")]
        public int StockMaximo { get; set; } = 100;

        [Column("punto_reorden")]
        [Range(0, int.MaxValue, ErrorMessage = "El punto de reorden debe ser mayor o igual a cero.")]
        [Display(Name = "Punto de Reorden")]
        public int PuntoReorden { get; set; } = 5;

        [Column("es_perecedero")]
        [Display(Name = "Es Perecedero")]
        public bool EsPerecedero { get; set; } = false;

        [Column("vida_util_dias")]
        [Range(1, int.MaxValue, ErrorMessage = "La vida útil debe ser mayor a cero.")]
        [Display(Name = "Vida Útil (días)")]
        public int? VidaUtilDias { get; set; }

        [Column("temperatura_almacenamiento")]
        [StringLength(30)]
        [Display(Name = "Temperatura de Almacenamiento")]
        public string? TemperaturaAlmacenamiento { get; set; }

        [Column("es_controlado")]
        [Display(Name = "Es Controlado")]
        public bool EsControlado { get; set; } = false;

        [Column("proveedor_principal_id")]
        [Display(Name = "Proveedor Principal")]
        public long? ProveedorPrincipalId { get; set; }

        [Column("tiempo_entrega_dias")]
        [Range(0, 365, ErrorMessage = "El tiempo de entrega debe estar entre 0 y 365 días.")]
        [Display(Name = "Tiempo de Entrega (días)")]
        public int TiempoEntregaDias { get; set; } = 7;

        [Column("peso_gramos")]
        [Precision(8, 2)]
        [Range(0, double.MaxValue, ErrorMessage = "El peso debe ser mayor o igual a cero.")]
        [Display(Name = "Peso (gramos)")]
        public decimal? PesoGramos { get; set; }

        [Column("dimensiones_cm")]
        [StringLength(30)]
        [Display(Name = "Dimensiones (cm)")]
        public string? DimensionesCm { get; set; }

        [Column("color")]
        [StringLength(30)]
        [Display(Name = "Color")]
        public string? Color { get; set; }

        [Column("es_destacado")]
        [Display(Name = "Es Destacado")]
        public bool EsDestacado { get; set; } = false;

        [Column("es_novedad")]
        [Display(Name = "Es Novedad")]
        public bool EsNovedad { get; set; } = false;

        [Column("descuento_maximo")]
        [Precision(5, 2)]
        [Range(0, 100, ErrorMessage = "El descuento máximo debe estar entre 0% y 100%.")]
        [Display(Name = "Descuento Máximo (%)")]
        public decimal DescuentoMaximo { get; set; } = 10.00m;

        [Column("comision_venta")]
        [Precision(5, 2)]
        [Range(0, 100, ErrorMessage = "La comisión debe estar entre 0% y 100%.")]
        [Display(Name = "Comisión de Venta (%)")]
        public decimal ComisionVenta { get; set; } = 5.00m;

        [Column("ingredientes_principales")]
        [Display(Name = "Ingredientes Principales")]
        public string? IngredientesPrincipales { get; set; }

        [Column("modo_uso")]
        [Display(Name = "Modo de Uso")]
        public string? ModoUso { get; set; }

        [Column("advertencias")]
        [Display(Name = "Advertencias")]
        public string? Advertencias { get; set; }

        [Column("imagen_principal_url")]
        [StringLength(255)]
        [Display(Name = "URL de Imagen Principal")]
        public string? ImagenPrincipalUrl { get; set; }

        [Column("imagenes_adicionales", TypeName = "jsonb")]
        public string ImagenesAdicionales { get; set; } = "[]";

        [Column("tags", TypeName = "jsonb")]
        public string Tags { get; set; } = "[]";

        [Column("palabras_clave")]
        [Display(Name = "Palabras Clave")]
        public string? PalabrasClave { get; set; }

        [Column("configuracion_adicional", TypeName = "jsonb")]
        public string ConfiguracionAdicional { get; set; } = "{}";

        [Column("is_active")]
        [Display(Name = "Producto Activo")]
        public bool IsActive { get; set; } = true;

        [Column("fecha_descontinuacion")]
        [Display(Name = "Fecha de Descontinuación")]
        public DateOnly? FechaDescontinuacion { get; set; }

        [Column("motivo_descontinuacion")]
        [Display(Name = "Motivo de Descontinuación")]
        public string? MotivoDescontinuacion { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("created_by")]
        public long? CreatedBy { get; set; }

        // Propiedades de navegación
        [ForeignKey("ProveedorPrincipalId")]
        [InverseProperty("ProductosProveidos")]
        public virtual Proveedor? ProveedorPrincipal { get; set; }

        [ForeignKey("CreatedBy")]
        [InverseProperty("ProductosCreados")]
        public virtual Usuario? UsuarioCreador { get; set; }

        [InverseProperty("Producto")]
        public virtual ICollection<ProductoSucursal> SucursalesAsignadas { get; set; } = new List<ProductoSucursal>();

        // Propiedades calculadas
        [NotMapped]
        [Display(Name = "Estado")]
        public string EstadoDescripcion => IsActive ? "Activo" : "Inactivo";

        [NotMapped]
        [Display(Name = "Precio Formateado")]
        public string PrecioFormateado => PrecioBase.ToString("C");

        [NotMapped]
        [Display(Name = "Contenido Formateado")]
        public string ContenidoFormateado => ContenidoNeto.HasValue 
            ? $"{ContenidoNeto.Value} {UnidadMedida}" 
            : UnidadMedida;

        [NotMapped]
        [Display(Name = "Nombre Completo")]
        public string NombreCompleto => $"{Marca} {Nombre}";

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

        [NotMapped]
        public List<string> ImagenesAdicionalesList
        {
            get
            {
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<string>>(ImagenesAdicionales) ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }
        }

        [NotMapped]
        public bool RequiereControlInventario => RequiereInventario && !EsPerecedero;

        [NotMapped]
        public string CategorizacionCompleta => string.IsNullOrEmpty(Subcategoria) 
            ? Categoria 
            : $"{Categoria} > {Subcategoria}";

        // Constantes para categorías
        public static class Categorias
        {
            public const string CuidadoCapilar = "Cuidado Capilar";
            public const string Peinado = "Peinado";
            public const string Tratamientos = "Tratamientos";
            public const string Coloracion = "Coloración";
            public const string Herramientas = "Herramientas";
            public const string Accesorios = "Accesorios";
            public const string Limpieza = "Limpieza";
            public const string Desinfeccion = "Desinfección";

            public static List<string> Lista => new()
            {
                CuidadoCapilar, Peinado, Tratamientos, Coloracion, 
                Herramientas, Accesorios, Limpieza, Desinfeccion
            };
        }

        // Constantes para subcategorías
        public static class Subcategorias
        {
            public const string Shampoos = "Shampoos";
            public const string Acondicionadores = "Acondicionadores";
            public const string Mascarillas = "Mascarillas";
            public const string Aceites = "Aceites";
            public const string Geles = "Geles";
            public const string Mousses = "Mousses";
            public const string Sprays = "Sprays";
            public const string Tintes = "Tintes";
            public const string Decolorantes = "Decolorantes";
            public const string Tijeras = "Tijeras";
            public const string Cepillos = "Cepillos";
            public const string Peines = "Peines";
            public const string Ligas = "Ligas";
            public const string Pinzas = "Pinzas";

            public static List<string> Lista => new()
            {
                Shampoos, Acondicionadores, Mascarillas, Aceites, Geles, Mousses, 
                Sprays, Tintes, Decolorantes, Tijeras, Cepillos, Peines, Ligas, Pinzas
            };
        }

        // Constantes para tipos de producto
        public static class TiposProducto
        {
            public const string Fisico = "Físico";
            public const string Digital = "Digital";
            public const string Servicio = "Servicio";

            public static List<string> Lista => new() { Fisico, Digital, Servicio };
        }

        // Constantes para unidades de medida
        public static class UnidadesMedida
        {
            public const string Pieza = "Pieza";
            public const string Mililitro = "ml";
            public const string Gramo = "gr";
            public const string Kilogramo = "kg";
            public const string Litro = "litro";
            public const string Pack = "Pack";
            public const string Set = "Set";

            public static List<string> Lista => new() 
            { 
                Pieza, Mililitro, Gramo, Kilogramo, Litro, Pack, Set 
            };
        }
    }
}