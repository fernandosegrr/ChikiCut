using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChikiCut.web.Data.Entities;

[Table("sucursal", Schema = "app")]
[Index("Code", Name = "branch_code_key", IsUnique = true)]
[Index("Code", Name = "ix_branch_code")]
[Index("IsActive", Name = "ix_branch_is_active")]
[Index("Rfc", Name = "ix_branch_rfc")]
public partial class Sucursal
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("code")]
    [StringLength(32)]
    public string Code { get; set; } = null!;

    [Column("name")]
    [StringLength(120)]
    public string Name { get; set; } = null!;

    [Column("razon_social")]
    [StringLength(160)]
    public string RazonSocial { get; set; } = null!;

    [Column("rfc")]
    [StringLength(13)]
    public string Rfc { get; set; } = null!;

    [Column("phone")]
    [StringLength(20)]
    public string Phone { get; set; } = null!;

    [Column("email")]
    [StringLength(120)]
    public string? Email { get; set; }

    [Column("address_line1")]
    [StringLength(160)]
    public string AddressLine1 { get; set; } = null!;

    [Column("address_line2")]
    [StringLength(160)]
    public string? AddressLine2 { get; set; }

    [Column("city")]
    [StringLength(80)]
    public string City { get; set; } = null!;

    [Column("state")]
    [StringLength(80)]
    public string State { get; set; } = null!;

    [Column("postal_code")]
    [StringLength(12)]
    public string PostalCode { get; set; } = null!;

    [Column("country")]
    [StringLength(2)]
    public string Country { get; set; } = null!;

    [Column("opening_hours", TypeName = "jsonb")]
    public string OpeningHours { get; set; } = null!;

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Propiedad de navegación para empleados
    [InverseProperty("Sucursal")]
    public virtual ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();

    // NUEVA NAVEGACIÓN PARA USUARIOS ASIGNADOS
    [InverseProperty("Sucursal")]
    public virtual ICollection<UsuarioSucursal> UsuariosAsignados { get; set; } = new List<UsuarioSucursal>();

    // NUEVA NAVEGACIÓN PARA SERVICIOS ASIGNADOS
    [InverseProperty("Sucursal")]
    public virtual ICollection<ServicioSucursal> ServiciosAsignados { get; set; } = new List<ServicioSucursal>();

    // NUEVA NAVEGACIÓN PARA PRODUCTOS ASIGNADOS
    [InverseProperty("Sucursal")]
    public virtual ICollection<ProductoSucursal> ProductosAsignados { get; set; } = new List<ProductoSucursal>();

    // NUEVAS PROPIEDADES CALCULADAS
    [NotMapped]
    public List<Usuario> UsuariosActivos => UsuariosAsignados
        .Where(us => us.IsActive && us.Usuario.IsActive)
        .Select(us => us.Usuario)
        .ToList();

    [NotMapped]
    public int CantidadUsuariosAsignados => UsuariosAsignados
        .Count(us => us.IsActive && us.Usuario.IsActive);

    [NotMapped]
    public string UsuariosNombres => string.Join(", ", 
        UsuariosActivos.Select(u => u.CodigoUsuario).OrderBy(codigo => codigo));

    // NUEVAS PROPIEDADES PARA SERVICIOS
    [NotMapped]
    public List<ServicioSucursal> ServiciosDisponibles => ServiciosAsignados
        .Where(ss => ss.IsActive && ss.Disponible && ss.Servicio.IsActive)
        .ToList();

    [NotMapped]
    public int CantidadServiciosDisponibles => ServiciosDisponibles.Count;

    [NotMapped]
    public decimal PrecioPromedioServicios => ServiciosDisponibles.Any() 
        ? ServiciosDisponibles.Average(ss => ss.PrecioLocal) 
        : 0m;

    // NUEVAS PROPIEDADES PARA PRODUCTOS
    [NotMapped]
    public List<ProductoSucursal> ProductosDisponibles => ProductosAsignados
        .Where(ps => ps.IsActive && ps.Disponible && ps.Producto.IsActive)
        .ToList();

    [NotMapped]
    public int CantidadProductosDisponibles => ProductosDisponibles.Count;

    [NotMapped]
    public decimal PrecioPromedioProductos => ProductosDisponibles.Any() 
        ? ProductosDisponibles.Average(ps => ps.PrecioVenta) 
        : 0m;

    [NotMapped]
    public int StockTotalProductos => ProductosDisponibles
        .Where(ps => ps.Producto.RequiereInventario)
        .Sum(ps => ps.StockActual);
}
