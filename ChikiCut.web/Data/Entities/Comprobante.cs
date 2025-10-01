using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChikiCut.web.Data.Entities
{
    [Table("comprobante")]
    public class Comprobante
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("tipo")]
        public string Tipo { get; set; } = string.Empty;

        [Column("archivo_url")]
        public string ArchivoUrl { get; set; } = string.Empty;

        [Column("creado_en")]
        public DateTime CreadoEn { get; set; }
    }
}
