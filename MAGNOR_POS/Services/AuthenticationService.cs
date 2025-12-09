using MAGNOR_POS.Data;
using MAGNOR_POS.Models;
using Microsoft.EntityFrameworkCore;

namespace MAGNOR_POS.Services;

public class AuthenticationService
{
    private readonly AppDbContext _context;
    private User? _currentUser;

    public User? CurrentUser => _currentUser;

    public AuthenticationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message, User? User)> LoginAsync(string username, string password)
    {
        try
        {
            // Find user by username
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return (false, "Usuario no encontrado", null);
            }

            // Check if user is active
            if (!user.IsActive)
            {
                return (false, "Usuario inactivo. Contacte al administrador.", null);
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return (false, "Contraseña incorrecta", null);
            }

            // Update last login
            user.LastLogin = DateTime.Now;
            await _context.SaveChangesAsync();

            // Set current user
            _currentUser = user;

            return (true, "Login exitoso", user);
        }
        catch (Exception ex)
        {
            return (false, $"Error al iniciar sesión: {ex.Message}", null);
        }
    }

    public void Logout()
    {
        _currentUser = null;
    }

    public bool IsAuthenticated()
    {
        return _currentUser != null;
    }

    public bool HasRole(RoleType roleType)
    {
        return _currentUser?.RoleId == (int)roleType;
    }
}
