using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;
using Pracrice.Modeles;

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

    public virtual DbSet<InvestmentCompany> InvestmentCompanies { get; set; }

    public virtual DbSet<InvestmentRate> InvestmentRates { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductRecipe> ProductRecipes { get; set; }

    public virtual DbSet<Resource> Resources { get; set; }

    public virtual DbSet<Store> Stores { get; set; }

    public virtual DbSet<StoreWarehouseDistance> StoreWarehouseDistances { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    public virtual DbSet<WarehouseStock> WarehouseStocks { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=127.0.0.1;port=3306;user=root;database=RosPiv", Microsoft.EntityFrameworkCore.ServerVersion.Parse("10.8.4-mariadb"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_general_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<InvestmentCompany>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("Investment_companies")
                .UseCollation("utf8mb4_unicode_ci");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<InvestmentRate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("Investment_rates")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.CompanyId, "fk_rates_company");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.CompanyId)
                .HasColumnType("int(11)")
                .HasColumnName("company_id");
            entity.Property(e => e.MaxAmount)
                .HasPrecision(15, 2)
                .HasColumnName("max_amount");
            entity.Property(e => e.MinAmount)
                .HasPrecision(15, 2)
                .HasColumnName("min_amount");

            entity.HasOne(d => d.Company).WithMany(p => p.InvestmentRates)
                .HasForeignKey(d => d.CompanyId)
                .HasConstraintName("fk_rates_company");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.Sku, "sku").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasPrecision(15, 2)
                .HasColumnName("price");
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .HasColumnName("sku");
        });

        modelBuilder.Entity<ProductRecipe>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.ResourceId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity
                .ToTable("Product_recipes")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.ResourceId, "fk_recipes_resource");

            entity.Property(e => e.ProductId)
                .HasColumnType("int(11)")
                .HasColumnName("product_id");
            entity.Property(e => e.ResourceId)
                .HasColumnType("int(11)")
                .HasColumnName("resource_id");
            entity.Property(e => e.Consumption)
                .HasPrecision(10, 4)
                .HasColumnName("consumption");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductRecipes)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_recipes_product");

            entity.HasOne(d => d.Resource).WithMany(p => p.ProductRecipes)
                .HasForeignKey(d => d.ResourceId)
                .HasConstraintName("fk_recipes_resource");
        });

        modelBuilder.Entity<Resource>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Unit)
                .HasMaxLength(20)
                .HasColumnName("unit");
        });

        modelBuilder.Entity<Store>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
        });

        modelBuilder.Entity<StoreWarehouseDistance>(entity =>
        {
            entity.HasKey(e => new { e.StoreId, e.WarehouseId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity
                .ToTable("Store_warehouse_distances")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.WarehouseId, "fk_distances_warehouse");

            entity.Property(e => e.StoreId)
                .HasColumnType("int(11)")
                .HasColumnName("store_id");
            entity.Property(e => e.WarehouseId)
                .HasColumnType("int(11)")
                .HasColumnName("warehouse_id");
            entity.Property(e => e.DistanceKm)
                .HasPrecision(8, 2)
                .HasColumnName("distance_km");

            entity.HasOne(d => d.Store).WithMany(p => p.StoreWarehouseDistances)
                .HasForeignKey(d => d.StoreId)
                .HasConstraintName("fk_distances_store");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.StoreWarehouseDistances)
                .HasForeignKey(d => d.WarehouseId)
                .HasConstraintName("fk_distances_warehouse");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.Login, "login").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .HasColumnName("full_name");
            entity.Property(e => e.Login)
                .HasMaxLength(50)
                .HasColumnName("login");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(64)
                .HasColumnName("password_hash");
            entity.Property(e => e.Role)
                .HasColumnType("enum('administrator','director','manager')")
                .HasColumnName("role");
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.UseCollation("utf8mb4_unicode_ci");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<WarehouseStock>(entity =>
        {
            entity.HasKey(e => new { e.WarehouseId, e.ProductId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity
                .ToTable("Warehouse_stocks")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.ProductId, "fk_stocks_product");

            entity.Property(e => e.WarehouseId)
                .HasColumnType("int(11)")
                .HasColumnName("warehouse_id");
            entity.Property(e => e.ProductId)
                .HasColumnType("int(11)")
                .HasColumnName("product_id");
            entity.Property(e => e.Quantity)
                .HasColumnType("int(11)")
                .HasColumnName("quantity");

            entity.HasOne(d => d.Product).WithMany(p => p.WarehouseStocks)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_stocks_product");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.WarehouseStocks)
                .HasForeignKey(d => d.WarehouseId)
                .HasConstraintName("fk_stocks_warehouse");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
