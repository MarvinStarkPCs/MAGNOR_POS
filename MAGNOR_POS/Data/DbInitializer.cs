using MAGNOR_POS.Models;
using Microsoft.EntityFrameworkCore;

namespace MAGNOR_POS.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        // Delete and recreate database if schema changed
        // IMPORTANT: In production, use migrations instead
        try
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }
        catch
        {
            // If deletion fails, try to create anyway
            context.Database.EnsureCreated();
        }

        // Check if we already have users
        if (context.Users.Any())
        {
            return; // DB has been seeded
        }

        // Create default admin user
        var defaultAdmin = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), // Default password: admin123
            FullName = "Administrador",
            Email = "admin@magnorpos.com",
            IsActive = true,
            RoleId = (int)RoleType.Administrador,
            CreatedAt = DateTime.Now
        };

        context.Users.Add(defaultAdmin);
        context.SaveChanges();
    }
}
