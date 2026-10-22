using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Software_SARA.Repositories.Models;

public partial class SarabdContext : DbContext
{
    public SarabdContext()
    {
    }

    public SarabdContext(DbContextOptions<SarabdContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DiaCaja> DiaCajas { get; set; }

    public virtual DbSet<Factura> Facturas { get; set; }

    public virtual DbSet<Proveedor> Proveedors { get; set; }

    public virtual DbSet<Recordatorio> Recordatorios { get; set; }

    public virtual DbSet<Rol> Rols { get; set; }

    public virtual DbSet<Sede> Sedes { get; set; }

    public virtual DbSet<Servicio> Servicios { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DiaCaja>(entity =>
        {
            entity.HasKey(e => e.IdDiaCaja).HasName("PK_dia");

            entity.ToTable("dia_caja");

            entity.Property(e => e.IdDiaCaja).HasColumnName("id_dia_caja");
            entity.Property(e => e.EstadoDia).HasColumnName("estado_dia");
            entity.Property(e => e.FechaHoraApertura)
                .HasColumnType("datetime")
                .HasColumnName("fecha_hora_apertura");
            entity.Property(e => e.FechaHoraCierre)
                .HasColumnType("datetime")
                .HasColumnName("fecha_hora_cierre");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.MontoFinal)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("monto_final");
            entity.Property(e => e.MontoInicial)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("monto_inicial");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.DiaCajas)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_usuario_dia_caja");
        });

        modelBuilder.Entity<Factura>(entity =>
        {
            entity.HasKey(e => e.IdFactura);

            entity.ToTable("factura");

            entity.Property(e => e.IdFactura).HasColumnName("id_factura");
            entity.Property(e => e.EstadoFactura).HasColumnName("estado_factura");
            entity.Property(e => e.FechaRegFactura).HasColumnName("fecha_reg_factura");
            entity.Property(e => e.FechaVencimiento).HasColumnName("fecha_vencimiento");
            entity.Property(e => e.IdProveedor).HasColumnName("id_proveedor");
            entity.Property(e => e.IdServicio).HasColumnName("id_servicio");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.TipFactura).HasColumnName("tip_factura");
            entity.Property(e => e.ValorFactura)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("valor_factura");

            entity.HasOne(d => d.IdProveedorNavigation).WithMany(p => p.Facturas)
                .HasForeignKey(d => d.IdProveedor)
                .HasConstraintName("FK_proveedor_factura");

            entity.HasOne(d => d.IdServicioNavigation).WithMany(p => p.Facturas)
                .HasForeignKey(d => d.IdServicio)
                .HasConstraintName("FK_servicio_factura");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Facturas)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_usuario_factura");
        });

        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.HasKey(e => e.IdProveedor);

            entity.ToTable("proveedor");

            entity.Property(e => e.IdProveedor).HasColumnName("id_proveedor");
            entity.Property(e => e.NomProveedor)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("nom_proveedor");
        });

        modelBuilder.Entity<Recordatorio>(entity =>
        {
            entity.HasKey(e => e.IdRecordatorio);

            entity.ToTable("recordatorio");

            entity.Property(e => e.IdRecordatorio).HasColumnName("id_recordatorio");
            entity.Property(e => e.EstadoRecordatorio).HasColumnName("estado_recordatorio");
            entity.Property(e => e.FechaEntrega).HasColumnName("fecha_entrega");
            entity.Property(e => e.FechaRegRecordatorio).HasColumnName("fecha_reg_recordatorio");
            entity.Property(e => e.IdProveedor).HasColumnName("id_proveedor");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");

            entity.HasOne(d => d.IdProveedorNavigation).WithMany(p => p.Recordatorios)
                .HasForeignKey(d => d.IdProveedor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_proveedor_recordatorio");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Recordatorios)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_usuario_recordatorio");
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.IdRol);

            entity.ToTable("rol");

            entity.Property(e => e.IdRol).HasColumnName("id_rol");
            entity.Property(e => e.NomRol)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("nom_rol");
        });

        modelBuilder.Entity<Sede>(entity =>
        {
            entity.HasKey(e => e.IdSede);

            entity.ToTable("sede");

            entity.Property(e => e.IdSede).HasColumnName("id_sede");
            entity.Property(e => e.BarrioSede)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("barrio_sede");
            entity.Property(e => e.CiuSede)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ciu_sede");
            entity.Property(e => e.DepSede)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("dep_sede");
            entity.Property(e => e.DirSede)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("dir_sede");
            entity.Property(e => e.EstadoSede).HasColumnName("estado_sede");
            entity.Property(e => e.NomSede)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("nom_sede");
            entity.Property(e => e.TelSede)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("tel_sede");
        });

        modelBuilder.Entity<Servicio>(entity =>
        {
            entity.HasKey(e => e.IdServicio);

            entity.ToTable("servicio");

            entity.Property(e => e.IdServicio).HasColumnName("id_servicio");
            entity.Property(e => e.NomServicio)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("nom_servicio");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario);

            entity.ToTable("usuario");

            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.ApelliUsuario)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("apelli_usuario");
            entity.Property(e => e.Contraseña)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("contraseña");
            entity.Property(e => e.Correo)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("correo");
            entity.Property(e => e.DirUsuario)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("dir_usuario");
            entity.Property(e => e.EstadoUsuario).HasColumnName("estado_usuario");
            entity.Property(e => e.FechaNaci).HasColumnName("fecha_naci");
            entity.Property(e => e.IdRol).HasColumnName("id_rol");
            entity.Property(e => e.IdSede).HasColumnName("id_sede");
            entity.Property(e => e.NomUsuario)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("nom_usuario");
            entity.Property(e => e.NumDoc).HasColumnName("num_doc");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("telefono");
            entity.Property(e => e.TipDoc).HasColumnName("tip_doc");

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdRol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_rol_usuario");

            entity.HasOne(d => d.IdSedeNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdSede)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_sede_usuario");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
