using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data.Interfaces;

namespace ChikiCut.web.Data.Entities
{
    [Table("producto_sucursal", Schema = "app")]
    [Index("ProductoId", Name = "ix_producto_sucursal_producto_id")]
    [Index("SucursalId", Name = "ix_producto_sucursal_sucursal_id")]
    [Index("Disponible", Name = "ix_producto_sucursal_disponible")]
    [Index("IsActive", Name = "ix_producto_sucursal_active")]
    public partial class ProductoSucursal : ITieneSucursal
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("producto_id")]
        [Required]
        [Display(Name = "Producto")]
        public long ProductoId { get; set; }

        [Column("sucursal_id")]
        [Required]
        [Display(Name = "Sucursal")]
        public long SucursalId { get; set; }

        [Column("precio_venta")]
        [Precision(10, 2)]
        [Range(0, double.MaxValue, ErrorMessage = "El precio de venta debe ser mayor o igual a cero.")]
        [Required(ErrorMessage = "El precio de venta es obligatorio.")]
        [Display(Name = "Precio de Venta")]
        public decimal PrecioVenta { get; set; }

        [Column("precio_mayoreo")]
        [Precision(10, 2)]
        [Range(0, double.MaxValue, ErrorMessage = "El precio de mayoreo debe ser mayor o igual a cero.")]
        [Display(Name = "Precio de Mayoreo")]
        public decimal? PrecioMayoreo { get; set; }

        [Column("cantidad_mayoreo")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad de mayoreo debe ser mayor a cero.")]
        [Display(Name = "Cantidad para Mayoreo")]
        public int CantidadMayoreo { get; set; } = 10;

        [Column("costo_local")]
        [Precision(10, 2)]
        [Range(0, double.MaxValue, ErrorMessage = "El costo local debe ser mayor o igual a cero.")]
        [Display(Name = "Costo Local")]
        public decimal? CostoLocal { get; set; }

        [Column("stock_actual")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock actual debe ser mayor o igual a cero.")]
        [Display(Name = "Stock Actual")]
        public int StockActual { get; set; } = 0;

        [Column("stock_minimo_local")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo debe ser mayor o igual a cero.")]
        [Display(Name = "Stock Mínimo Local")]
        public int? StockMinimoLocal { get; set; }

        [Column("stock_maximo_local")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock máximo debe ser mayor o igual a cero.")]
        [Display(Name = "Stock Máximo Local")]
        public int? StockMaximoLocal { get; set; }

        [Column("punto_reorden_local")]
        [Range(0, int.MaxValue, ErrorMessage = "El punto de reorden debe ser mayor o igual a cero.")]
        [Display(Name = "Punto de Reorden Local")]
        public int? PuntoReordenLocal { get; set; }

        [Column("disponible")]
        [Display(Name = "Disponible")]
        public bool Disponible { get; set; } = true;

        [Column("se_puede_vender")]
        [Display(Name = "Se Puede Vender")]
        public bool SePuedeVender { get; set; } = true;

        [Column("se_puede_reservar")]
        [Display(Name = "Se Puede Reservar")]
        public bool SePuedeReservar { get; set; } = true;

        [Column("requiere_autorizacion")]
        [Display(Name = "Requiere Autorización")]
        public bool RequiereAutorizacion { get; set; } = false;

        [Column("descuento_maximo_local")]
        [Precision(5, 2)]
        [Range(0, 100, ErrorMessage = "El descuento máximo debe estar entre 0% y 100%.")]
        [Display(Name = "Descuento Máximo Local (%)")]
        public decimal DescuentoMaximoLocal { get; set; } = 0.00m;

        [Column("en_promocion")]
        [Display(Name = "En Promoción")]
        public bool EnPromocion { get; set; } = false;

        [Column("precio_promocion")]
        [Precision(10, 2)]
        [Range(0, double.MaxValue, ErrorMessage = "El precio de promoción debe ser mayor o igual a cero.")]
        [Display(Name = "Precio de Promoción")]
        public decimal? PrecioPromocion { get; set; }

        [Column("fecha_inicio_promocion")]
        [Display(Name = "Fecha Inicio Promoción")]
        public DateOnly? FechaInicioPromocion { get; set; }

        [Column("fecha_fin_promocion")]
        [Display(Name = "Fecha Fin Promoción")]
        public DateOnly? FechaFinPromocion { get; set; }

        [Column("orden_display")]
        [Display(Name = "Orden de Visualización")]
        public int OrdenDisplay { get; set; } = 0;

        [Column("es_destacado_local")]
        [Display(Name = "Es Destacado Local")]
        public bool EsDestacadoLocal { get; set; } = false;

        [Column("ubicacion_fisica")]
        [StringLength(100)]
        [Display(Name = "Ubicación Física")]
        public string? UbicacionFisica { get; set; }

        [Column("seccion_display")]
        [StringLength(50)]
        [Display(Name = "Sección de Display")]
        public string? SeccionDisplay { get; set; }

        [Column("observaciones_locales")]
        [Display(Name = "Observaciones Locales")]
        public string? ObservacionesLocales { get; set; }

        [Column("notas_venta")]
        [Display(Name = "Notas de Venta")]
        public string? NotasVenta { get; set; }

        [Column("fecha_ultimo_inventario")]
        [Display(Name = "Fecha Último Inventario")]
        public DateOnly? FechaUltimoInventario { get; set; }

        [Column("fecha_ultima_venta")]
        [Display(Name = "Fecha Última Venta")]
        public DateOnly? FechaUltimaVenta { get; set; }

        [Column("fecha_ultimo_ingreso")]
        [Display(Name = "Fecha Último Ingreso")]
        public DateOnly? FechaUltimoIngreso { get; set; }

        [Column("total_vendido")]
        [Range(0, int.MaxValue, ErrorMessage = "El total vendido debe ser mayor o igual a cero.")]
        [Display(Name = "Total Vendido")]
        public int TotalVendido { get; set; } = 0;

        [Column("ingresos_generados")]
        [Precision(12, 2)]
        [Range(0, double.MaxValue, ErrorMessage = "Los ingresos generados deben ser mayor o igual a cero.")]
        [Display(Name = "Ingresos Generados")]
        public decimal IngresosGenerados { get; set; } = 0.00m;

        [Column("configuracion_local", TypeName = "jsonb")]
        public string ConfiguracionLocal { get; set; } = "{}";

        [Column("is_active")]
        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;

        [Column("fecha_descontinuacion_local")]
        [Display(Name = "Fecha Descontinuación Local")]
        public DateOnly? FechaDescontinuacionLocal { get; set; }

        [Column("motivo_descontinuacion_local")]
        [Display(Name = "Motivo Descontinuación Local")]
        public string? MotivoDescontinuacionLocal { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("created_by")]
        public long? CreatedBy { get; set; }

        // Propiedades de navegación
        [ForeignKey("ProductoId")]
        [InverseProperty("SucursalesAsignadas")]
        public virtual Producto Producto { get; set; } = null!;

        [ForeignKey("SucursalId")]
        [InverseProperty("ProductosAsignados")]
        public virtual Sucursal Sucursal { get; set; } = null!;

        [ForeignKey("CreatedBy")]
        [InverseProperty("ProductoSucursalesCreados")]
        public virtual Usuario? UsuarioCreador { get; set; }

        // Propiedades calculadas
        [NotMapped]
        [Display(Name = "Estado")]
        public string EstadoDescripcion => 
            !IsActive ? "Inactivo" 
            : !Disponible ? "No Disponible" 
            : !SePuedeVender ? "No se puede vender"
            : StockActual <= 0 && Producto?.RequiereInventario == true ? "Sin stock"
            : "Disponible";

        [NotMapped]
        [Display(Name = "Precio Efectivo")]
        public decimal PrecioEfectivo => 
            EnPromocion && PrecioPromocion.HasValue && EstaEnPromocionHoy 
                ? PrecioPromocion.Value 
                : PrecioVenta;

        [NotMapped]
        [Display(Name = "Stock Mínimo Efectivo")]
        public int StockMinimoEfectivo => StockMinimoLocal ?? Producto?.StockMinimo ?? 0;

        [NotMapped]
        [Display(Name = "Stock Máximo Efectivo")]
        public int StockMaximoEfectivo => StockMaximoLocal ?? Producto?.StockMaximo ?? 100;

        [NotMapped]
        [Display(Name = "Punto Reorden Efectivo")]
        public int PuntoReordenEfectivo => PuntoReordenLocal ?? Producto?.PuntoReorden ?? 5;

        [NotMapped]
        [Display(Name = "Precio Formateado")]
        public string PrecioFormateado => PrecioEfectivo.ToString("C");

        [NotMapped]
        [Display(Name = "Margen de Ganancia")]
        public decimal? MargenGanancia => 
            CostoLocal.HasValue && CostoLocal > 0 
                ? Math.Round(((PrecioVenta - CostoLocal.Value) / CostoLocal.Value) * 100, 2)
                : null;

        [NotMapped]
        public bool EstaEnPromocionHoy
        {
            get
            {
                if (!EnPromocion || !PrecioPromocion.HasValue) return false;
                
                var hoy = DateOnly.FromDateTime(DateTime.Today);
                return (FechaInicioPromocion == null || hoy >= FechaInicioPromocion) &&
                       (FechaFinPromocion == null || hoy <= FechaFinPromocion);
            }
        }

        [NotMapped]
        public bool EstaDisponibleParaVenta
        {
            get
            {
                return IsActive && Disponible && SePuedeVender &&
                       (Producto?.RequiereInventario != true || StockActual > 0);
            }
        }

        [NotMapped]
        public bool RequiereReorden
        {
            get
            {
                return Producto?.RequiereInventario == true && 
                       StockActual <= PuntoReordenEfectivo;
            }
        }

        [NotMapped]
        public string EstadoStock
        {
            get
            {
                if (Producto?.RequiereInventario != true) return "N/A";
                
                if (StockActual <= 0) return "Sin stock";
                if (RequiereReorden) return "Stock bajo";
                if (StockActual >= StockMaximoEfectivo) return "Stock alto";
                return "Stock normal";
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
            if (porcentajeDescuento > DescuentoMaximoLocal) porcentajeDescuento = DescuentoMaximoLocal;

            return PrecioEfectivo * (1 - porcentajeDescuento / 100);
        }

        // Método para verificar si puede aplicarse un descuento
        public bool PuedeAplicarDescuento(decimal porcentajeDescuento)
        {
            return porcentajeDescuento >= 0 && porcentajeDescuento <= DescuentoMaximoLocal;
        }

        // Método para actualizar stock
        public void ActualizarStock(int cantidad, string tipoMovimiento)
        {
            switch (tipoMovimiento.ToLower())
            {
                case "venta":
                    StockActual = Math.Max(0, StockActual - Math.Abs(cantidad));
                    FechaUltimaVenta = DateOnly.FromDateTime(DateTime.Today);
                    TotalVendido += Math.Abs(cantidad);
                    IngresosGenerados += Math.Abs(cantidad) * PrecioEfectivo;
                    break;
                case "ingreso":
                    StockActual += Math.Abs(cantidad);
                    FechaUltimoIngreso = DateOnly.FromDateTime(DateTime.Today);
                    break;
                case "ajuste":
                    StockActual = Math.Max(0, cantidad);
                    FechaUltimoInventario = DateOnly.FromDateTime(DateTime.Today);
                    break;
            }
        }

        // Constantes para secciones de display
        public static class SeccionesDisplay
        {
            public const string Mostrador = "Mostrador";
            public const string Vitrina = "Vitrina";
            public const string Almacen = "Almacén";
            public const string Bodega = "Bodega";
            public const string Refrigerador = "Refrigerador";
            public const string Estanteria = "Estantería";

            public static List<string> Lista => new()
            {
                Mostrador, Vitrina, Almacen, Bodega, Refrigerador, Estanteria
            };
        }
    }
}