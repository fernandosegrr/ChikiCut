using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChikiCut.web.Data.Entities;

[Table("puesto", Schema = "app")]
[Index("Nombre", Name = "puesto_nombre_key", IsUnique = true)]
[Index("IsActive", Name = "ix_puesto_is_active")]
[Index("NivelJerarquico", Name = "ix_puesto_nivel_jerarquico")]
public partial class Puesto
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("nombre")]
    [StringLength(80)]
    [Required(ErrorMessage = "El campo Nombre del Puesto es obligatorio.")]
    [Display(Name = "Nombre del Puesto")]
    public string Nombre { get; set; } = null!;

    [Column("descripcion")]
    [StringLength(200)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Column("salario_base_minimo")]
    [Precision(10, 2)]
    [Display(Name = "Salario Base Mínimo")]
    [Range(0, double.MaxValue, ErrorMessage = "El salario mínimo debe ser mayor o igual a cero.")]
    public decimal? SalarioBaseMinimo { get; set; }

    [Column("salario_base_maximo")]
    [Precision(10, 2)]
    [Display(Name = "Salario Base Máximo")]
    [Range(0, double.MaxValue, ErrorMessage = "El salario máximo debe ser mayor o igual a cero.")]
    public decimal? SalarioBaseMaximo { get; set; }

    [Column("requiere_experiencia")]
    [Display(Name = "Requiere Experiencia")]
    public bool RequiereExperiencia { get; set; }

    [Column("nivel_jerarquico")]
    [Required]
    [Display(Name = "Nivel Jerárquico")]
    [Range(1, 4, ErrorMessage = "El nivel jerárquico debe estar entre 1 y 4.")]
    public int NivelJerarquico { get; set; } = 1;

    [Column("is_active")]
    [Display(Name = "Puesto Activo")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Propiedad de navegación
    [InverseProperty("PuestoNavegacion")]
    public virtual ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();

    // Propiedades calculadas
    [NotMapped]
    [Display(Name = "Nivel")]
    public string NivelDescripcion => NivelJerarquico switch
    {
        1 => "Operativo",
        2 => "Especialista", 
        3 => "Supervisor",
        4 => "Gerencial",
        _ => "Sin definir"
    };

    [NotMapped]
    public string RangoSalarial => SalarioBaseMinimo.HasValue && SalarioBaseMaximo.HasValue 
        ? $"${SalarioBaseMinimo:N0} - ${SalarioBaseMaximo:N0}"
        : "No definido";
}