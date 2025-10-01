using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChikiCut.web.Data.Entities
{
    [Table("cliente")]
    public class Cliente
    {
        [Column("id")]
        public long Id { get; set; }
        [Column("nombre_completo")]
        public string NombreCompleto { get; set; } = string.Empty;
        [Column("telefono")]
        public string? Telefono { get; set; }
        [Column("email")]
        public string Email { get; set; } = string.Empty;
        [Column("fecha_nacimiento")]
        public DateOnly? FechaNacimiento { get; set; }
        [Column("genero")]
        public string? Genero { get; set; }
        [Column("direccion")]
        public string? Direccion { get; set; }
        [Column("ciudad")]
        public string? Ciudad { get; set; }
        [Column("estado")]
        public string? Estado { get; set; }
        [Column("codigo_postal")]
        public string? CodigoPostal { get; set; }
        [Column("pais")]
        public string? Pais { get; set; }
        [Column("notas")]
        public string? Notas { get; set; }
        [Column("is_active")]
        public bool IsActive { get; set; } = true;
        [Column("created_at")]
        public DateOnly CreatedAt { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        [Column("updated_at")]
        public DateOnly? UpdatedAt { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    }
}