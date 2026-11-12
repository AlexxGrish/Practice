using Microsoft.EntityFrameworkCore;
using Pracrice.Models;

namespace Pracrice.Data;

public sealed class AppDbContext : DbContext
{
    private readonly string _connectionString;
    private readonly ServerVersion _serverVersion;

    public AppDbContext(string connectionString, ServerVersion serverVersion)
    {
        _connectionString = connectionString;
        _serverVersion = serverVersion;
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Resource> Resources => Set<Resource>();

    public DbSet<ProductRecipe> ProductRecipes => Set<ProductRecipe>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    public DbSet<Store> Stores => Set<Store>();

    public DbSet<StoreWarehouseDistance> StoreWarehouseDistances => Set<StoreWarehouseDistance>();

    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();

    public DbSet<InvestmentCompany> InvestmentCompanies => Set<InvestmentCompany>();

    public DbSet<InvestmentRate> InvestmentRates => Set<InvestmentRate>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        optionsBuilder.UseMySql(_connectionString, _serverVersion, options =>
        {
            options.EnableRetryOnFailure(2);
            options.CommandTimeout(30);
        });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);

            entity.Property(user => user.Id).HasColumnName("id");
            entity.Property(user => user.Login).HasColumnName("login").HasMaxLength(50);
            entity.Property(user => user.PasswordHash).HasColumnName("password_hash").HasMaxLength(100);
            entity.Property(user => user.Role)
                .HasColumnName("role")
                .HasMaxLength(20)
                .HasConversion(
                    role => role.ToString().ToLowerInvariant(),
                    value => Enum.Parse<UserRole>(value, true));
            entity.Property(user => user.FullName).HasColumnName("full_name").HasMaxLength(150);

            entity.HasIndex(user => user.Login, "login").IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(product => product.Id);

            entity.Property(product => product.Id).HasColumnName("id");
            entity.Property(product => product.Name).HasColumnName("name").HasMaxLength(150);
            entity.Property(product => product.Sku).HasColumnName("sku").HasMaxLength(50);
            entity.Property(product => product.Price).HasColumnName("price").HasPrecision(15, 2);
            entity.Property(product => product.ImagePath).HasColumnName("image_path").HasMaxLength(255);

            entity.HasIndex(product => product.Sku, "sku").IsUnique();
        });

        modelBuilder.Entity<Resource>(entity =>
        {
            entity.ToTable("Resources");
            entity.HasKey(resource => resource.Id);

            entity.Property(resource => resource.Id).HasColumnName("id");
            entity.Property(resource => resource.Name).HasColumnName("name").HasMaxLength(100);
            entity.Property(resource => resource.Unit).HasColumnName("unit").HasMaxLength(20);
        });

        modelBuilder.Entity<ProductRecipe>(entity =>
        {
            entity.ToTable("Product_recipes");
            entity.HasKey(recipe => new { recipe.ProductId, recipe.ResourceId });

            entity.Property(recipe => recipe.ProductId).HasColumnName("product_id");
            entity.Property(recipe => recipe.ResourceId).HasColumnName("resource_id");
            entity.Property(recipe => recipe.Consumption).HasColumnName("consumption").HasPrecision(10, 4);

            entity.HasOne(recipe => recipe.Product)
                .WithMany(product => product.ProductRecipes)
                .HasForeignKey(recipe => recipe.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_recipes_product");

            entity.HasOne(recipe => recipe.Resource)
                .WithMany(resource => resource.ProductRecipes)
                .HasForeignKey(recipe => recipe.ResourceId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_recipes_resource");
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.ToTable("Warehouses");
            entity.HasKey(warehouse => warehouse.Id);

            entity.Property(warehouse => warehouse.Id).HasColumnName("id");
            entity.Property(warehouse => warehouse.Name).HasColumnName("name").HasMaxLength(100);
            entity.Property(warehouse => warehouse.Address).HasColumnName("address").HasMaxLength(255);
        });

        modelBuilder.Entity<Store>(entity =>
        {
            entity.ToTable("Stores");
            entity.HasKey(store => store.Id);

            entity.Property(store => store.Id).HasColumnName("id");
            entity.Property(store => store.Name).HasColumnName("name").HasMaxLength(100);
            entity.Property(store => store.Address).HasColumnName("address").HasMaxLength(255);
            entity.Property(store => store.Phone).HasColumnName("phone").HasMaxLength(20);
            entity.Property(store => store.Demand).HasColumnName("demand").HasDefaultValue(0);
        });

        modelBuilder.Entity<StoreWarehouseDistance>(entity =>
        {
            entity.ToTable("Store_warehouse_distances");
            entity.HasKey(distance => new { distance.StoreId, distance.WarehouseId });

            entity.Property(distance => distance.StoreId).HasColumnName("store_id");
            entity.Property(distance => distance.WarehouseId).HasColumnName("warehouse_id");
            entity.Property(distance => distance.DistanceKm).HasColumnName("distance_km").HasPrecision(8, 2);

            entity.HasOne(distance => distance.Store)
                .WithMany(store => store.StoreWarehouseDistances)
                .HasForeignKey(distance => distance.StoreId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_distances_store");

            entity.HasOne(distance => distance.Warehouse)
                .WithMany(warehouse => warehouse.StoreWarehouseDistances)
                .HasForeignKey(distance => distance.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_distances_warehouse");
        });

        modelBuilder.Entity<WarehouseStock>(entity =>
        {
            entity.ToTable("Warehouse_stocks");
            entity.HasKey(stock => new { stock.WarehouseId, stock.ProductId });

            entity.Property(stock => stock.WarehouseId).HasColumnName("warehouse_id");
            entity.Property(stock => stock.ProductId).HasColumnName("product_id");
            entity.Property(stock => stock.Quantity).HasColumnName("quantity");

            entity.HasOne(stock => stock.Warehouse)
                .WithMany(warehouse => warehouse.WarehouseStocks)
                .HasForeignKey(stock => stock.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_stocks_warehouse");

            entity.HasOne(stock => stock.Product)
                .WithMany(product => product.WarehouseStocks)
                .HasForeignKey(stock => stock.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_stocks_product");
        });

        modelBuilder.Entity<InvestmentCompany>(entity =>
        {
            entity.ToTable("Investment_companies");
            entity.HasKey(company => company.Id);

            entity.Property(company => company.Id).HasColumnName("id");
            entity.Property(company => company.Name).HasColumnName("name").HasMaxLength(100);
        });

        modelBuilder.Entity<InvestmentRate>(entity =>
        {
            entity.ToTable("Investment_rates");
            entity.HasKey(rate => rate.Id);

            entity.Property(rate => rate.Id).HasColumnName("id");
            entity.Property(rate => rate.CompanyId).HasColumnName("company_id");
            entity.Property(rate => rate.MinAmount).HasColumnName("min_amount").HasPrecision(15, 2);
            entity.Property(rate => rate.MaxAmount).HasColumnName("max_amount").HasPrecision(15, 2);

            entity.HasOne(rate => rate.Company)
                .WithMany(company => company.InvestmentRates)
                .HasForeignKey(rate => rate.CompanyId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_rates_company");
        });
    }
}
