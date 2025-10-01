using System;
using System.Collections.Generic;
using ChikiCut.web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ChikiCut.web.Data;

public partial class AppDbContext : DbContext
{
    // Usar el constructor principal con el parámetro 'options'
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public virtual DbSet<Sucursal> Sucursals { get; set; }
    public virtual DbSet<Empleado> Empleados { get; set; }
    public virtual DbSet<Puesto> Puestos { get; set; }
    public virtual DbSet<Usuario> Usuarios { get; set; }
    public virtual DbSet<Rol> Roles { get; set; }
    public virtual DbSet<Proveedor> Proveedores { get; set; }
    public virtual DbSet<RoleTemplate> RoleTemplates { get; set; }
    public virtual DbSet<ConceptoGasto> ConceptosGasto { get; set; }
    public virtual DbSet<UsuarioSucursal> UsuarioSucursales { get; set; }
    public virtual DbSet<Servicio> Servicios { get; set; }
    public virtual DbSet<ServicioSucursal> ServicioSucursales { get; set; }
    public virtual DbSet<Producto> Productos { get; set; }
    public virtual DbSet<ProductoSucursal> ProductoSucursales { get; set; }
    public virtual DbSet<Cliente> Clientes { get; set; }
    public virtual DbSet<Gasto> Gastos { get; set; }
    public virtual DbSet<Comprobante> Comprobantes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cliente_pkey");
            entity.Property(e => e.CreatedAt).HasColumnType("date");
            entity.Property(e => e.UpdatedAt).HasColumnType("date");
            entity.Property(e => e.FechaNacimiento).HasColumnType("date");
            // ...otros mapeos y restricciones...
        });

        modelBuilder.Entity<Sucursal>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("branch_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("nextval('branch_id_seq'::regclass)");
            entity.Property(e => e.Country)
                .HasDefaultValueSql("'MX'::bpchar")
                .IsFixedLength();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.OpeningHours).HasDefaultValueSql("'{}'::jsonb");
        });

        modelBuilder.Entity<Empleado>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("empleado_pkey");

            entity.Property(e => e.Pais).HasDefaultValue("MX");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Especialidades).HasDefaultValueSql("'[]'::jsonb");
            entity.Property(e => e.HorarioTrabajo).HasDefaultValueSql("'{}'::jsonb");
            entity.Property(e => e.Comisiones).HasDefaultValueSql("'{}'::jsonb");

            // Configurar la relación con Sucursal
            entity.HasOne(d => d.Sucursal)
                  .WithMany(p => p.Empleados)
                  .HasForeignKey(d => d.SucursalId)
                  .HasConstraintName("empleado_sucursal_fk");

            // Configurar la relación con Puesto usando PuestoId
            entity.HasOne(d => d.PuestoNavegacion)
                  .WithMany(p => p.Empleados)
                  .HasForeignKey(d => d.PuestoId)
                  .HasConstraintName("empleado_puesto_fk");
        });

        modelBuilder.Entity<Puesto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("puesto_pkey");
            
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.NivelJerarquico).HasDefaultValue(1);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("usuario_pkey");
            
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IntentosFallidos).HasDefaultValue(0);

            // Configurar la relación con Empleado
            entity.HasOne(d => d.Empleado)
                  .WithOne(p => p.Usuario)
                  .HasForeignKey<Usuario>(d => d.EmpleadoId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("fk_usuario_empleado");

            // Configurar la relación con Rol
            entity.HasOne(d => d.Rol)
                  .WithMany(p => p.Usuarios)
                  .HasForeignKey(d => d.RolId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("fk_usuario_rol");

            // Configurar la relación de usuario creador
            entity.HasOne(d => d.UsuarioCreador)
                  .WithMany(p => p.UsuariosCreados)
                  .HasForeignKey(d => d.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("fk_usuario_created_by");
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("rol_pkey");
            
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Permisos).HasDefaultValueSql("'{}'::jsonb");
        });

        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("proveedor_pkey");
            
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.TiposProductos).HasDefaultValueSql("'[]'::jsonb");
            entity.Property(e => e.CondicionesPago).HasDefaultValue("Contado");
            entity.Property(e => e.DiasCredito).HasDefaultValue(0);
            entity.Property(e => e.DescuentoProntoPago).HasDefaultValue(0.00m);
            entity.Property(e => e.Moneda).HasDefaultValue("MXN");
            entity.Property(e => e.Categoria).HasDefaultValue("General");
            entity.Property(e => e.Pais).HasDefaultValue("MX");
        });

        modelBuilder.Entity<RoleTemplate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("role_template_pkey");
            
            entity.ToTable("role_template", "app"); // Especificar nombre de tabla y esquema
            
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.PermissionsJson).HasColumnName("permissions_json");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsFavorite).HasColumnName("is_favorite");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsFavorite).HasDefaultValue(false);

            // Relación con Usuario creador
            entity.HasOne(d => d.Creator)
                  .WithMany()
                  .HasForeignKey(d => d.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("fk_role_template_created_by");
        });

        modelBuilder.Entity<ConceptoGasto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("conceptos_gasto_pkey");
            
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Categoria).HasDefaultValue("General");
            entity.Property(e => e.TipoFrecuencia).HasDefaultValue("Ocasional");
            entity.Property(e => e.RequiereComprobante).HasDefaultValue(true);
            entity.Property(e => e.RequiereAutorizacion).HasDefaultValue(false);
            entity.Property(e => e.NivelAutorizacionRequerido).HasDefaultValue(1);
            entity.Property(e => e.AplicaTodasSucursales).HasDefaultValue(true);
            entity.Property(e => e.SucursalesAplicables).HasDefaultValueSql("'[]'::jsonb");
            entity.Property(e => e.Tags).HasDefaultValueSql("'[]'::jsonb");
            entity.Property(e => e.ConfiguracionAdicional).HasDefaultValueSql("'{}'::jsonb");

            // Configurar la relación con Usuario creador
            entity.HasOne(d => d.UsuarioCreador)
                  .WithMany(p => p.ConceptosGastoCreados)
                  .HasForeignKey(d => d.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("fk_conceptos_gasto_created_by");
        });

        modelBuilder.Entity<UsuarioSucursal>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("usuario_sucursal_pkey");
            
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.FechaAsignacion).HasDefaultValueSql("now()");

            // Configurar relación con Usuario
            entity.HasOne(d => d.Usuario)
                  .WithMany(p => p.SucursalesAsignadas)
                  .HasForeignKey(d => d.UsuarioId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("fk_usuario_sucursal_usuario");

            // Configurar relación con Sucursal
            entity.HasOne(d => d.Sucursal)
                  .WithMany(p => p.UsuariosAsignados)
                  .HasForeignKey(d => d.SucursalId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("fk_usuario_sucursal_sucursal");

            // Configurar relación con Usuario creador
            entity.HasOne(d => d.UsuarioCreador)
                  .WithMany(p => p.AsignacionesSucursalCreadas)
                  .HasForeignKey(d => d.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("fk_usuario_sucursal_created_by");

            // Índice único para evitar duplicados
            entity.HasIndex(e => new { e.UsuarioId, e.SucursalId })
                  .IsUnique()
                  .HasDatabaseName("uk_usuario_sucursal_unique");
        });

        modelBuilder.Entity<Servicio>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("servicio_pkey");
            
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Categoria).HasDefaultValue("General");
            entity.Property(e => e.DuracionEstimada).HasDefaultValue(30);
            entity.Property(e => e.PrecioBase).HasDefaultValue(0.00m);
            entity.Property(e => e.RequiereCita).HasDefaultValue(true);
            entity.Property(e => e.NivelDificultad).HasDefaultValue(1);
            entity.Property(e => e.ComisionEmpleado).HasDefaultValue(10.00m);
            entity.Property(e => e.Tags).HasDefaultValueSql("'[]'::jsonb");
            entity.Property(e => e.ConfiguracionAdicional).HasDefaultValueSql("'{}'::jsonb");

            // Configurar la relación con Usuario creador
            entity.HasOne(d => d.UsuarioCreador)
                  .WithMany(p => p.ServiciosCreados)
                  .HasForeignKey(d => d.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("fk_servicio_created_by");

            // Índice único para código
            entity.HasIndex(e => e.Codigo)
                  .IsUnique()
                  .HasDatabaseName("uk_servicio_codigo");
        });

        modelBuilder.Entity<ServicioSucursal>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("servicio_sucursal_pkey");
            
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Disponible).HasDefaultValue(true);
            entity.Property(e => e.DescuentoMaximo).HasDefaultValue(0.00m);
            entity.Property(e => e.OrdenDisplay).HasDefaultValue(0);
            entity.Property(e => e.ConfiguracionLocal).HasDefaultValueSql("'{}'::jsonb");

            // Configurar relación con Servicio
            entity.HasOne(d => d.Servicio)
                  .WithMany(p => p.SucursalesAsignadas)
                  .HasForeignKey(d => d.ServicioId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("fk_servicio_sucursal_servicio");

            // Configurar relación con Sucursal
            entity.HasOne(d => d.Sucursal)
                  .WithMany(p => p.ServiciosAsignados)
                  .HasForeignKey(d => d.SucursalId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("fk_servicio_sucursal_sucursal");

            // Configurar relación con Usuario creador
            entity.HasOne(d => d.UsuarioCreador)
                  .WithMany(p => p.ServicioSucursalesCreados)
                  .HasForeignKey(d => d.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("fk_servicio_sucursal_created_by");

            // Índice único para evitar duplicados
            entity.HasIndex(e => new { e.ServicioId, e.SucursalId })
                  .IsUnique()
                  .HasDatabaseName("uk_servicio_sucursal_unique");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("producto_pkey");
            
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Categoria).HasDefaultValue("General");
            entity.Property(e => e.TipoProducto).HasDefaultValue("Físico");
            entity.Property(e => e.UnidadMedida).HasDefaultValue("Pieza");
            entity.Property(e => e.PrecioBase).HasDefaultValue(0.00m);
            entity.Property(e => e.MargenGananciaSugerido).HasDefaultValue(30.00m);
            entity.Property(e => e.RequiereInventario).HasDefaultValue(true);
            entity.Property(e => e.StockMinimo).HasDefaultValue(0);
            entity.Property(e => e.StockMaximo).HasDefaultValue(100);
            entity.Property(e => e.PuntoReorden).HasDefaultValue(5);
            entity.Property(e => e.EsPerecedero).HasDefaultValue(false);
            entity.Property(e => e.EsControlado).HasDefaultValue(false);
            entity.Property(e => e.TiempoEntregaDias).HasDefaultValue(7);
            entity.Property(e => e.EsDestacado).HasDefaultValue(false);
            entity.Property(e => e.EsNovedad).HasDefaultValue(false);
            entity.Property(e => e.DescuentoMaximo).HasDefaultValue(10.00m);
            entity.Property(e => e.ComisionVenta).HasDefaultValue(5.00m);
            entity.Property(e => e.ImagenesAdicionales).HasDefaultValueSql("'[]'::jsonb");
            entity.Property(e => e.Tags).HasDefaultValueSql("'[]'::jsonb");
            entity.Property(e => e.ConfiguracionAdicional).HasDefaultValueSql("'{}'::jsonb");

            // Configurar relación con Proveedor
            entity.HasOne(d => d.ProveedorPrincipal)
                  .WithMany(p => p.ProductosProveidos)
                  .HasForeignKey(d => d.ProveedorPrincipalId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("fk_producto_proveedor");

            // Configurar la relación con Usuario creador
            entity.HasOne(d => d.UsuarioCreador)
                  .WithMany(p => p.ProductosCreados)
                  .HasForeignKey(d => d.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("fk_producto_created_by");

            // Índice único para código
            entity.HasIndex(e => e.Codigo)
                  .IsUnique()
                  .HasDatabaseName("uk_producto_codigo");
        });

        modelBuilder.Entity<ProductoSucursal>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("producto_sucursal_pkey");
            
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Disponible).HasDefaultValue(true);
            entity.Property(e => e.SePuedeVender).HasDefaultValue(true);
            entity.Property(e => e.SePuedeReservar).HasDefaultValue(true);
            entity.Property(e => e.RequiereAutorizacion).HasDefaultValue(false);
            entity.Property(e => e.DescuentoMaximoLocal).HasDefaultValue(0.00m);
            entity.Property(e => e.EnPromocion).HasDefaultValue(false);
            entity.Property(e => e.OrdenDisplay).HasDefaultValue(0);
            entity.Property(e => e.EsDestacadoLocal).HasDefaultValue(false);
            entity.Property(e => e.StockActual).HasDefaultValue(0);
            entity.Property(e => e.CantidadMayoreo).HasDefaultValue(10);
            entity.Property(e => e.TotalVendido).HasDefaultValue(0);
            entity.Property(e => e.IngresosGenerados).HasDefaultValue(0.00m);
            entity.Property(e => e.ConfiguracionLocal).HasDefaultValueSql("'{}'::jsonb");

            // Configurar relación con Producto
            entity.HasOne(d => d.Producto)
                  .WithMany(p => p.SucursalesAsignadas)
                  .HasForeignKey(d => d.ProductoId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("fk_producto_sucursal_producto");

            // Configurar relación con Sucursal
            entity.HasOne(d => d.Sucursal)
                  .WithMany(p => p.ProductosAsignados)
                  .HasForeignKey(d => d.SucursalId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("fk_producto_sucursal_sucursal");

            // Configurar relación con Usuario creador
            entity.HasOne(d => d.UsuarioCreador)
                  .WithMany(p => p.ProductoSucursalesCreados)
                  .HasForeignKey(d => d.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("fk_producto_sucursal_created_by");

            // Índice único para evitar duplicados
            entity.HasIndex(e => new { e.ProductoId, e.SucursalId })
                  .IsUnique()
                  .HasDatabaseName("uk_producto_sucursal_unique");
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cliente_pkey");
            
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.NombreCompleto).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Telefono).HasMaxLength(15);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
            entity.Property(e => e.FechaNacimiento).HasColumnType("date");
            entity.Property(e => e.Genero).HasMaxLength(20);
            entity.Property(e => e.Direccion).HasMaxLength(255);
            entity.Property(e => e.Ciudad).HasMaxLength(100);
            entity.Property(e => e.Estado).HasMaxLength(100);
            entity.Property(e => e.CodigoPostal).HasMaxLength(20);
            entity.Property(e => e.Pais).HasMaxLength(50);
            entity.Property(e => e.Notas).HasMaxLength(500);
            entity.Property(e => e.UpdatedAt).HasColumnType("date");
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("uk_cliente_email");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    public override int SaveChanges()
    {
        StampDates();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampDates();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void StampDates()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        foreach (var entry in ChangeTracker.Entries<Cliente>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                    entry.Entity.CreatedAt = today;
                entry.Entity.UpdatedAt = today;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = today;
            }
        }
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
