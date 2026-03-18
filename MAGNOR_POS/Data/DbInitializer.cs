using MAGNOR_POS.Models;
using MAGNOR_POS.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace MAGNOR_POS.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        // Create database if it doesn't exist (DO NOT delete existing data)
        context.Database.EnsureCreated();

        // Apply schema updates for existing databases
        ApplySchemaUpdates(context);

        // Check if we already have users
        if (context.Users.Any())
        {
            // DB already seeded, just make sure categories/products exist
            SeedProductsIfEmpty(context);
            return;
        }

        // Create default admin user
        var defaultAdmin = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            FullName = "Administrador",
            Email = "admin@magnorpos.com",
            IsActive = true,
            RoleId = (int)RoleType.Administrador,
            CreatedAt = DateTime.Now
        };

        context.Users.Add(defaultAdmin);
        context.SaveChanges();

        // Seed categories and products
        SeedProductsIfEmpty(context);
    }

    private static void SeedProductsIfEmpty(AppDbContext context)
    {
        // Only seed if no categories exist
        if (context.Categories.Any())
            return;

        // Create categories
        var catBebidas = new Category
        {
            Name = "Bebidas",
            CreatedAt = DateTime.Now,
            IsActive = true
        };
        var catAlimentos = new Category
        {
            Name = "Alimentos",
            CreatedAt = DateTime.Now,
            IsActive = true
        };
        var catSnacks = new Category
        {
            Name = "Snacks",
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        context.Categories.AddRange(catBebidas, catAlimentos, catSnacks);
        context.SaveChanges();

        // Create sample products
        var products = new List<Product>
        {
            new Product
            {
                Name = "Coca Cola 500ml",
                SKU = "BEB001",
                Barcode = "7501234567890",
                SalePrice = 3500m,
                PurchasePrice = 2000m,
                CurrentStock = 50,
                MinimumStock = 10,
                TaxRate = 0.19m,
                IsTaxable = true,
                TrackStock = true,
                CategoryId = catBebidas.Id,
                ImageUrl = "🥤",
                CreatedAt = DateTime.Now,
                IsActive = true
            },
            new Product
            {
                Name = "Postobon Manzana 400ml",
                SKU = "BEB002",
                Barcode = "7501234567891",
                SalePrice = 2800m,
                PurchasePrice = 1500m,
                CurrentStock = 45,
                MinimumStock = 10,
                TaxRate = 0.19m,
                IsTaxable = true,
                TrackStock = true,
                CategoryId = catBebidas.Id,
                ImageUrl = "🥤",
                CreatedAt = DateTime.Now,
                IsActive = true
            },
            new Product
            {
                Name = "Agua Cristal 600ml",
                SKU = "BEB003",
                Barcode = "7501234567892",
                SalePrice = 2000m,
                PurchasePrice = 1000m,
                CurrentStock = 100,
                MinimumStock = 20,
                TaxRate = 0.19m,
                IsTaxable = true,
                TrackStock = true,
                CategoryId = catBebidas.Id,
                ImageUrl = "💧",
                CreatedAt = DateTime.Now,
                IsActive = true
            },
            new Product
            {
                Name = "Jugo Hit 350ml",
                SKU = "BEB004",
                Barcode = "7501234567899",
                SalePrice = 3000m,
                PurchasePrice = 1800m,
                CurrentStock = 40,
                MinimumStock = 10,
                TaxRate = 0.19m,
                IsTaxable = true,
                TrackStock = true,
                CategoryId = catBebidas.Id,
                ImageUrl = "🧃",
                CreatedAt = DateTime.Now,
                IsActive = true
            },
            new Product
            {
                Name = "Sandwich de Pollo",
                SKU = "ALI001",
                Barcode = "7501234567893",
                SalePrice = 8500m,
                PurchasePrice = 4500m,
                CurrentStock = 20,
                MinimumStock = 5,
                TaxRate = 0.19m,
                IsTaxable = true,
                TrackStock = true,
                CategoryId = catAlimentos.Id,
                ImageUrl = "🥪",
                CreatedAt = DateTime.Now,
                IsActive = true
            },
            new Product
            {
                Name = "Hamburguesa Clasica",
                SKU = "ALI002",
                Barcode = "7501234567894",
                SalePrice = 12000m,
                PurchasePrice = 6000m,
                CurrentStock = 15,
                MinimumStock = 5,
                TaxRate = 0.19m,
                IsTaxable = true,
                TrackStock = true,
                CategoryId = catAlimentos.Id,
                ImageUrl = "🍔",
                CreatedAt = DateTime.Now,
                IsActive = true
            },
            new Product
            {
                Name = "Pizza Personal",
                SKU = "ALI003",
                Barcode = "7501234567895",
                SalePrice = 15000m,
                PurchasePrice = 7000m,
                CurrentStock = 10,
                MinimumStock = 3,
                TaxRate = 0.19m,
                IsTaxable = true,
                TrackStock = true,
                CategoryId = catAlimentos.Id,
                ImageUrl = "🍕",
                CreatedAt = DateTime.Now,
                IsActive = true
            },
            new Product
            {
                Name = "Empanada",
                SKU = "ALI004",
                Barcode = "7501234567900",
                SalePrice = 3000m,
                PurchasePrice = 1500m,
                CurrentStock = 30,
                MinimumStock = 10,
                TaxRate = 0.19m,
                IsTaxable = true,
                TrackStock = true,
                CategoryId = catAlimentos.Id,
                ImageUrl = "🥟",
                CreatedAt = DateTime.Now,
                IsActive = true
            },
            new Product
            {
                Name = "Papas Margarita",
                SKU = "SNK001",
                Barcode = "7501234567896",
                SalePrice = 4500m,
                PurchasePrice = 2500m,
                CurrentStock = 60,
                MinimumStock = 15,
                TaxRate = 0.19m,
                IsTaxable = true,
                TrackStock = true,
                CategoryId = catSnacks.Id,
                ImageUrl = "🍟",
                CreatedAt = DateTime.Now,
                IsActive = true
            },
            new Product
            {
                Name = "Galletas Festival",
                SKU = "SNK002",
                Barcode = "7501234567897",
                SalePrice = 5000m,
                PurchasePrice = 2800m,
                CurrentStock = 40,
                MinimumStock = 10,
                TaxRate = 0.19m,
                IsTaxable = true,
                TrackStock = true,
                CategoryId = catSnacks.Id,
                ImageUrl = "🍪",
                CreatedAt = DateTime.Now,
                IsActive = true
            },
            new Product
            {
                Name = "Chocolate Jet",
                SKU = "SNK003",
                Barcode = "7501234567898",
                SalePrice = 2500m,
                PurchasePrice = 1200m,
                CurrentStock = 80,
                MinimumStock = 20,
                TaxRate = 0.19m,
                IsTaxable = true,
                TrackStock = true,
                CategoryId = catSnacks.Id,
                ImageUrl = "🍫",
                CreatedAt = DateTime.Now,
                IsActive = true
            },
            new Product
            {
                Name = "Chicles Trident",
                SKU = "SNK004",
                Barcode = "7501234567901",
                SalePrice = 2000m,
                PurchasePrice = 1000m,
                CurrentStock = 100,
                MinimumStock = 25,
                TaxRate = 0.19m,
                IsTaxable = true,
                TrackStock = true,
                CategoryId = catSnacks.Id,
                ImageUrl = "🍬",
                CreatedAt = DateTime.Now,
                IsActive = true
            }
        };

        context.Products.AddRange(products);
        context.SaveChanges();
    }

    /// <summary>
    /// Applies schema updates to existing databases (adds missing columns)
    /// </summary>
    private static void ApplySchemaUpdates(AppDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        connection.Open();

        // Define columns to add if missing: (table, column, type, default)
        var columnsToAdd = new List<(string table, string column, string type, string defaultVal)>
        {
            // Factus columns for Sales table
            ("Sales", "FactusCUFE", "TEXT", "NULL"),
            ("Sales", "FactusQRCode", "TEXT", "NULL"),
            ("Sales", "FactusNumber", "TEXT", "NULL"),
            ("Sales", "FactusPrefix", "TEXT", "NULL"),
            ("Sales", "FactusStatus", "TEXT", "NULL"),
        };

        foreach (var (table, column, type, defaultVal) in columnsToAdd)
        {
            try
            {
                using var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name='{column}'";
                var exists = Convert.ToInt64(checkCmd.ExecuteScalar()) > 0;

                if (!exists)
                {
                    using var alterCmd = connection.CreateCommand();
                    alterCmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {type} DEFAULT {defaultVal}";
                    alterCmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // Column might already exist or table doesn't exist yet, skip
            }
        }

        connection.Close();
    }
}
