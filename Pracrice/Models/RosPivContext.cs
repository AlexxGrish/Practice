using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace Pracrice.Models;

public partial class RosPivContext : DbContext
{
    public RosPivContext()
    {
    }

    public RosPivContext(DbContextOptions<RosPivContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Equipment> Equipment { get; set; }

    public virtual DbSet<Investment> Investments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<Route> Routes { get; set; }

    public virtual DbSet<Shop> Shops { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=127.0.0.1;port=3306;database=RosPiv;user=root", Microsoft.EntityFrameworkCore.ServerVersion.Parse("10.8.4-mariadb"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasKey(e => e.EquipmentId).HasName("PRIMARY");

            entity.ToTable("equipment");

            entity.Property(e => e.EquipmentId)
                .ValueGeneratedNever()
                .HasColumnType("int(11)")
                .HasColumnName("equipment_id");
            entity.Property(e => e.Age)
                .HasColumnType("int(11)")
                .HasColumnName("age");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.NewPrice)
                .HasPrecision(15, 2)
                .HasColumnName("new_price");
            entity.Property(e => e.OldPrice)
                .HasPrecision(15, 2)
                .HasColumnName("old_price");
            entity.Property(e => e.Profit)
                .HasPrecision(15, 2)
                .HasColumnName("profit");
        });

        modelBuilder.Entity<Investment>(entity =>
        {
            entity.HasKey(e => e.InvestmentId).HasName("PRIMARY");

            entity.ToTable("investments");

            entity.Property(e => e.InvestmentId)
                .ValueGeneratedNever()
                .HasColumnType("int(11)")
                .HasColumnName("investment_id");
            entity.Property(e => e.ProfitMatrix)
                .HasColumnType("text")
                .HasColumnName("profit_matrix");
            entity.Property(e => e.StepResource)
                .HasColumnType("int(11)")
                .HasColumnName("step_resource");
            entity.Property(e => e.TotalResources)
                .HasColumnType("int(11)")
                .HasColumnName("total_resources");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PRIMARY");

            entity.ToTable("products");

            entity.Property(e => e.ProductId)
                .ValueGeneratedNever()
                .HasColumnType("int(11)")
                .HasColumnName("product_id");
            entity.Property(e => e.ChoiceTable)
                .HasColumnType("text")
                .HasColumnName("choice_table");
            entity.Property(e => e.Demand)
                .HasPrecision(12, 2)
                .HasColumnName("demand");
            entity.Property(e => e.DpTable)
                .HasColumnType("text")
                .HasColumnName("dp_table");
            entity.Property(e => e.MaxInventory)
                .HasPrecision(12, 2)
                .HasColumnName("max_inventory");
            entity.Property(e => e.MaxProduction)
                .HasPrecision(12, 2)
                .HasColumnName("max_production");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PRIMARY");

            entity.ToTable("reports");

            entity.HasIndex(e => e.RouteId, "route_id");

            entity.HasIndex(e => e.UserId, "user_id");

            entity.Property(e => e.ReportId)
                .ValueGeneratedNever()
                .HasColumnType("int(11)")
                .HasColumnName("report_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.ReportType)
                .HasColumnType("int(11)")
                .HasColumnName("report_type");
            entity.Property(e => e.RouteId)
                .HasColumnType("int(11)")
                .HasColumnName("route_id");
            entity.Property(e => e.UserId)
                .HasColumnType("int(11)")
                .HasColumnName("user_id");

            entity.HasOne(d => d.Route).WithMany(p => p.Reports)
                .HasForeignKey(d => d.RouteId)
                .HasConstraintName("reports_ibfk_2");

            entity.HasOne(d => d.User).WithMany(p => p.Reports)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("reports_ibfk_1");
        });

        modelBuilder.Entity<Route>(entity =>
        {
            entity.HasKey(e => e.RouteId).HasName("PRIMARY");

            entity.ToTable("routes");

            entity.HasIndex(e => e.ProductId, "product_id");

            entity.HasIndex(e => e.ShopId, "shop_id");

            entity.HasIndex(e => e.WarehouseId, "warehouse_id");

            entity.Property(e => e.RouteId)
                .ValueGeneratedNever()
                .HasColumnType("int(11)")
                .HasColumnName("route_id");
            entity.Property(e => e.ProductId)
                .HasColumnType("int(11)")
                .HasColumnName("product_id");
            entity.Property(e => e.ShopId)
                .HasColumnType("int(11)")
                .HasColumnName("shop_id");
            entity.Property(e => e.WarehouseId)
                .HasColumnType("int(11)")
                .HasColumnName("warehouse_id");

            entity.HasOne(d => d.Product).WithMany(p => p.Routes)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("routes_ibfk_3");

            entity.HasOne(d => d.Shop).WithMany(p => p.Routes)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("routes_ibfk_1");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.Routes)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("routes_ibfk_2");
        });

        modelBuilder.Entity<Shop>(entity =>
        {
            entity.HasKey(e => e.ShopId).HasName("PRIMARY");

            entity.ToTable("shops");

            entity.Property(e => e.ShopId)
                .ValueGeneratedNever()
                .HasColumnType("int(11)")
                .HasColumnName("shop_id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.AvgAttendance)
                .HasPrecision(10, 2)
                .HasColumnName("avg_attendance");
            entity.Property(e => e.NumberOfCounters)
                .HasColumnType("int(11)")
                .HasColumnName("number_of_counters");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");

            entity.ToTable("users");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnType("int(11)")
                .HasColumnName("user_id");
            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .HasColumnName("full_name");
            entity.Property(e => e.LastChanges)
                .HasColumnType("timestamp")
                .HasColumnName("last_changes");
            entity.Property(e => e.Login)
                .HasMaxLength(50)
                .HasColumnName("login");
            entity.Property(e => e.Pass)
                .HasMaxLength(255)
                .HasColumnName("pass");
            entity.Property(e => e.Role)
                .HasColumnType("int(11)")
                .HasColumnName("role");
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.WarehouseId).HasName("PRIMARY");

            entity.ToTable("warehouses");

            entity.Property(e => e.WarehouseId)
                .ValueGeneratedNever()
                .HasColumnType("int(11)")
                .HasColumnName("warehouse_id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
            entity.Property(e => e.Resources)
                .HasColumnType("int(11)")
                .HasColumnName("resources");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
