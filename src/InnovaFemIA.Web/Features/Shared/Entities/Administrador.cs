namespace InnovaFemIA.Web.Features.Shared.Entities;

/// <summary>
/// Cuenta de administrador del sistema (equipo ICESI).
/// Tiene acceso al panel de administración de los 3 módulos.
/// </summary>
public class Administrador
{
    public int Id { get; set; }

    public string NombreUsuario { get; set; } = string.Empty;

    /// <summary>Hash BCrypt de la contraseña.</summary>
    public string HashContrasena { get; set; } = string.Empty;

    /// <summary>Nombre completo del administrador para mostrar en el panel.</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public DateTime? UltimoLogin { get; set; }
}
