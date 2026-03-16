using Microsoft.EntityFrameworkCore;
using MAGNOR_POS.Data;
using MAGNOR_POS.Models;
using BCrypt.Net;

namespace MAGNOR_POS.Services;

/// <summary>
/// Service for managing users and their roles
/// </summary>
public class UserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all users with their roles
    /// </summary>
    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Users
            .Include(u => u.Role)
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    /// <summary>
    /// Get active users only
    /// </summary>
    public async Task<List<User>> GetActiveUsersAsync()
    {
        return await _context.Users
            .Include(u => u.Role)
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    /// <summary>
    /// Get user by username
    /// </summary>
    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    /// <summary>
    /// Get all available roles
    /// </summary>
    public async Task<List<Role>> GetAllRolesAsync()
    {
        return await _context.Roles
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    public async Task<(bool success, string message, User? user)> AddUserAsync(User user, string password, int createdByUserId)
    {
        try
        {
            // Validate username is unique
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == user.Username);

            if (existingUser != null)
            {
                return (false, "El nombre de usuario ya existe", null);
            }

            // Validate email is unique (if provided)
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var existingEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == user.Email);

                if (existingEmail != null)
                {
                    return (false, "El correo electrónico ya está registrado", null);
                }
            }

            // Validate role exists
            var roleExists = await _context.Roles.AnyAsync(r => r.Id == user.RoleId && r.IsActive);
            if (!roleExists)
            {
                return (false, "El rol seleccionado no existe o está inactivo", null);
            }

            // Hash password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

            // Set audit fields
            user.CreatedAt = DateTime.Now;
            user.CreatedBy = createdByUserId;
            user.IsActive = true;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Reload with role
            await _context.Entry(user).Reference(u => u.Role).LoadAsync();

            return (true, "Usuario creado exitosamente", user);
        }
        catch (Exception ex)
        {
            return (false, $"Error al crear el usuario: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    public async Task<(bool success, string message)> UpdateUserAsync(User user, int updatedByUserId, string? newPassword = null)
    {
        try
        {
            var existingUser = await _context.Users.FindAsync(user.Id);
            if (existingUser == null)
            {
                return (false, "Usuario no encontrado");
            }

            // Validate username is unique (excluding current user)
            var duplicateUsername = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == user.Username && u.Id != user.Id);

            if (duplicateUsername != null)
            {
                return (false, "El nombre de usuario ya existe");
            }

            // Validate email is unique (if provided, excluding current user)
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var duplicateEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == user.Email && u.Id != user.Id);

                if (duplicateEmail != null)
                {
                    return (false, "El correo electrónico ya está registrado");
                }
            }

            // Validate role exists
            var roleExists = await _context.Roles.AnyAsync(r => r.Id == user.RoleId && r.IsActive);
            if (!roleExists)
            {
                return (false, "El rol seleccionado no existe o está inactivo");
            }

            // Update fields
            existingUser.Username = user.Username;
            existingUser.FullName = user.FullName;
            existingUser.Email = user.Email;
            existingUser.RoleId = user.RoleId;
            existingUser.IsActive = user.IsActive;

            // Update password if provided
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            }

            // Set audit fields
            existingUser.UpdatedAt = DateTime.Now;
            existingUser.UpdatedBy = updatedByUserId;

            await _context.SaveChangesAsync();

            return (true, "Usuario actualizado exitosamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al actualizar el usuario: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete (deactivate) a user
    /// </summary>
    public async Task<(bool success, string message)> DeleteUserAsync(int userId, int deletedByUserId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return (false, "Usuario no encontrado");
            }

            // Don't allow deleting the last admin
            if (user.RoleId == (int)RoleType.Administrador)
            {
                var adminCount = await _context.Users
                    .CountAsync(u => u.RoleId == (int)RoleType.Administrador && u.IsActive);

                if (adminCount <= 1)
                {
                    return (false, "No se puede eliminar el último administrador del sistema");
                }
            }

            // Don't allow self-deletion
            if (userId == deletedByUserId)
            {
                return (false, "No puedes eliminar tu propio usuario");
            }

            // Soft delete
            user.IsActive = false;
            user.UpdatedAt = DateTime.Now;
            user.UpdatedBy = deletedByUserId;

            await _context.SaveChangesAsync();

            return (true, "Usuario desactivado exitosamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al eliminar el usuario: {ex.Message}");
        }
    }

    /// <summary>
    /// Change user password
    /// </summary>
    public async Task<(bool success, string message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return (false, "Usuario no encontrado");
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                return (false, "La contraseña actual es incorrecta");
            }

            // Validate new password
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                return (false, "La nueva contraseña debe tener al menos 6 caracteres");
            }

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return (true, "Contraseña actualizada exitosamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al cambiar la contraseña: {ex.Message}");
        }
    }

    /// <summary>
    /// Reset user password (admin function)
    /// </summary>
    public async Task<(bool success, string message, string? temporaryPassword)> ResetPasswordAsync(int userId, int resetByUserId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return (false, "Usuario no encontrado", null);
            }

            // Generate temporary password
            var tempPassword = GenerateTemporaryPassword();

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
            user.UpdatedAt = DateTime.Now;
            user.UpdatedBy = resetByUserId;

            await _context.SaveChangesAsync();

            return (true, "Contraseña restablecida exitosamente", tempPassword);
        }
        catch (Exception ex)
        {
            return (false, $"Error al restablecer la contraseña: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Get users by role
    /// </summary>
    public async Task<List<User>> GetUsersByRoleAsync(int roleId)
    {
        return await _context.Users
            .Include(u => u.Role)
            .Where(u => u.RoleId == roleId && u.IsActive)
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    /// <summary>
    /// Get user statistics
    /// </summary>
    public async Task<(int total, int active, int inactive, Dictionary<string, int> byRole)> GetUserStatisticsAsync()
    {
        var total = await _context.Users.CountAsync();
        var active = await _context.Users.CountAsync(u => u.IsActive);
        var inactive = total - active;

        var byRole = await _context.Users
            .Include(u => u.Role)
            .Where(u => u.IsActive)
            .GroupBy(u => u.Role!.Name)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Role, x => x.Count);

        return (total, active, inactive, byRole);
    }

    /// <summary>
    /// Generate a temporary password
    /// </summary>
    private string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
