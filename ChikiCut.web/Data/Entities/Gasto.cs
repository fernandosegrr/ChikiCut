using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChikiCut.web.Data.Entities
{
    [Table("gasto")]
    public class Gasto
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("sucursal_id")]
        [Required]
        public long SucursalId { get; set; }

        [Column("concepto_gasto_id")]
        [Required]
        public long ConceptoGastoId { get; set; }

        [Column("descripcion")]
        [MaxLength(255)]
        public string? Descripcion { get; set; }

        [Column("monto")]
        [Required]
        public decimal Monto { get; set; }

        [Column("fecha")]
        [Required]
        public DateOnly Fecha { get; set; }

        [Column("creado_por")]
        public long CreadoPor { get; set; }

        [Column("creado_en")]
        public DateTime CreadoEn { get; set; }

        [Column("updated_en")]
        public DateTime? UpdatedEn { get; set; }

        [Column("estatus")]
        [MaxLength(20)]
        public string? Estatus { get; set; }

        [Column("metodo_pago")]
        [MaxLength(50)]
        public string? MetodoPago { get; set; }

        [Column("comprobante_id")]
        public long? ComprobanteId { get; set; }

        [Column("observaciones")]
        [MaxLength(255)]
        public string? Observaciones { get; set; }
    }
}
