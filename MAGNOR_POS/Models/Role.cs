using MAGNOR_POS.Models.Base;

namespace MAGNOR_POS.Models;

public class Role : AuditableEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }

    // Navigation property
    public ICollection<User> Users { get; set; } = new List<User>();
}

// Enum for predefined roles
public enum RoleType
{
    Administrador = 1,
    Cajero = 2,
    Mesero = 3,
    Inventarios = 4,
    Supervisor = 5
}
