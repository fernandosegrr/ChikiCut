using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ChikiCut.web.Data.Interfaces;

namespace ChikiCut.web.Data.Entities;

[Table("empleado", Schema = "app")]
[Index("CodigoEmpleado", Name = "empleado_codigo_empleado_key", IsUnique = true)]
[Index("SucursalId", Name = "ix_empleado_sucursal_id")]
[Index("IsActive", Name = "ix_empleado_is_active")]
public partial class Empleado : ITieneSucursal
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("sucursal_id")]
    [Required(ErrorMessage = "Debe seleccionar una sucursal.")]
    [Display(Name = "Sucursal")]
    public long SucursalId { get; set; }

    [Column("puesto_id")]
    [Required(ErrorMessage = "Debe seleccionar un puesto.")]
    [Display(Name = "Puesto")]
    public long PuestoId { get; set; }

    [Column("codigo_empleado")]
    [StringLength(20)]   
    [Required(ErrorMessage = "El campo Código de Empleado es obligatorio.")]
    [Display(Name = "Código de Empleado")]
    public string CodigoEmpleado { get; set; } = null!;

    [Column("nombre")]
    [StringLength(80)]
    [Required(ErrorMessage = "El campo Nombre es obligatorio.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = null!;

    [Column("apellido_paterno")]
    [StringLength(80)]
    [Required(ErrorMessage = "El campo Apellido Paterno es obligatorio.")]
    [Display(Name = "Apellido Paterno")]
    public string ApellidoPaterno { get; set; } = null!;

    [Column("apellido_materno")]
    [StringLength(80)]
    [Display(Name = "Apellido Materno")]
    public string? ApellidoMaterno { get; set; }

    [Column("alias")]
    [StringLength(50)]
    [Display(Name = "Alias/Apodo")]
    public string? Alias { get; set; }

    [Column("telefono")]
    [StringLength(20)]
    [Required(ErrorMessage = "El campo Teléfono es obligatorio.")]
    [Display(Name = "Teléfono")]
    public string Telefono { get; set; } = null!;

    [Column("telefono_emergencia")]
    [StringLength(20)]
    [Display(Name = "Teléfono de Emergencia")]
    public string? TelefonoEmergencia { get; set; }

    [Column("email")]
    [StringLength(120)]
    [EmailAddress(ErrorMessage = "Debe ser un email válido.")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Column("fecha_nacimiento")]
    [Display(Name = "Fecha de Nacimiento")]
    public DateOnly? FechaNacimiento { get; set; }

    [Column("curp")]
    [StringLength(18)]
    [Display(Name = "CURP")]
    public string? Curp { get; set; }

    [Column("rfc")]
    [StringLength(13)]
    [Display(Name = "RFC")]
    public string? Rfc { get; set; }

    [Column("nss")]
    [StringLength(15)]
    [Display(Name = "NSS")]
    public string? Nss { get; set; }

    [Column("salario_base")]
    [Precision(10, 2)]
    [Display(Name = "Salario Base")]
    [Range(0, double.MaxValue, ErrorMessage = "El salario debe ser mayor o igual a cero.")]
    public decimal? SalarioBase { get; set; }

    [Column("fecha_ingreso")]
    [Required(ErrorMessage = "El campo Fecha de Ingreso es obligatorio.")]
    [Display(Name = "Fecha de Ingreso")]
    public DateOnly FechaIngreso { get; set; }

    [Column("fecha_baja")]
    [Display(Name = "Fecha de Baja")]
    public DateOnly? FechaBaja { get; set; }

    [Column("direccion_linea1")]
    [StringLength(200)]
    [Required(ErrorMessage = "El campo Dirección es obligatorio.")]
    [Display(Name = "Dirección")]
    public string DireccionLinea1 { get; set; } = null!;

    [Column("direccion_linea2")]
    [StringLength(200)]
    [Display(Name = "Dirección Línea 2")]
    public string? DireccionLinea2 { get; set; }

    [Column("ciudad")]
    [StringLength(100)]
    [Required(ErrorMessage = "El campo Ciudad es obligatorio.")]
    [Display(Name = "Ciudad")]
    public string Ciudad { get; set; } = null!;

    [Column("estado")]
    [StringLength(100)]
    [Required(ErrorMessage = "El campo Estado es obligatorio.")]
    [Display(Name = "Estado")]
    public string Estado { get; set; } = null!;

    [Column("codigo_postal")]
    [StringLength(10)]
    [Required(ErrorMessage = "El campo Código Postal es obligatorio.")]
    [Display(Name = "Código Postal")]
    public string CodigoPostal { get; set; } = null!;

    [Column("pais")]
    [StringLength(2)]
    [Display(Name = "País")]
    public string Pais { get; set; } = "MX";

    [Column("contacto_emergencia_nombre")]
    [StringLength(120)]
    [Display(Name = "Contacto de Emergencia")]
    public string? ContactoEmergenciaNombre { get; set; }

    [Column("contacto_emergencia_telefono")]
    [StringLength(20)]
    [Display(Name = "Teléfono de Emergencia")]
    public string? ContactoEmergenciaTelefono { get; set; }

    [Column("contacto_emergencia_parentesco")]
    [StringLength(50)]
    [Display(Name = "Parentesco")]
    public string? ContactoEmergenciaParentesco { get; set; }

    [Column("especialidades", TypeName = "jsonb")]
    public string Especialidades { get; set; } = "[]";

    [Column("horario_trabajo", TypeName = "jsonb")]
    public string HorarioTrabajo { get; set; } = "{}";

    [Column("comisiones", TypeName = "jsonb")]
    public string Comisiones { get; set; } = "{}";

    [Column("is_active")]
    [Display(Name = "Empleado Activo")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Propiedades de navegación
    [ForeignKey("SucursalId")]
    [InverseProperty("Empleados")]
    public virtual Sucursal Sucursal { get; set; } = null!;

    [ForeignKey("PuestoId")]
    [InverseProperty("Empleados")]
    public virtual Puesto PuestoNavegacion { get; set; } = null!;

    // Relación con Usuario (un empleado puede tener una cuenta de usuario)
    [InverseProperty("Empleado")]
    public virtual Usuario? Usuario { get; set; }

    // Propiedades calculadas
    [NotMapped]
    [Display(Name = "Nombre Completo")]
    public string NombreCompleto => $"{Nombre} {ApellidoPaterno}"
        + (!string.IsNullOrEmpty(ApellidoMaterno) ? $" {ApellidoMaterno}" : "");

    [NotMapped]
    [Display(Name = "Nombre para Mostrar")]
    public string NombreDisplay => !string.IsNullOrEmpty(Alias) ?
        $"{Nombre} \"{Alias}\" {ApellidoPaterno}" : NombreCompleto;

    [NotMapped]
    [Display(Name = "Puesto")]
    public string PuestoNombre => PuestoNavegacion?.Nombre ?? "Sin puesto asignado";
}