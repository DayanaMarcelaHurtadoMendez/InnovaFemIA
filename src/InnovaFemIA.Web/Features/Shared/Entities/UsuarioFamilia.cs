namespace InnovaFemIA.Web.Features.Shared.Entities;

/// <summary>
/// Cuenta de acceso de una familia campesina al Módulo 3 (Biocomercio).
/// El administrador crea estas cuentas. Las contraseñas se almacenan con BCrypt.
/// Implementa bloqueo por intentos fallidos (3 intentos → 5 minutos de bloqueo).
/// </summary>
public class UsuarioFamilia
{
    public int Id { get; set; }

    public int FamiliaId { get; set; }
    public Familia Familia { get; set; } = null!;

    /// <summary>Nombre de usuario para el login (único en el sistema).</summary>
    public string NombreUsuario { get; set; } = string.Empty;

    /// <summary>Hash BCrypt de la contraseña. Nunca se almacena texto plano.</summary>
    public string HashContrasena { get; set; } = string.Empty;

    /// <summary>Número de intentos de login fallidos consecutivos.</summary>
    public int IntentosFallidos { get; set; } = 0;

    /// <summary>
    /// Fecha/hora hasta la que el usuario está bloqueado (UTC).
    /// Null si no está bloqueado.
    /// </summary>
    public DateTime? BloqueadoHasta { get; set; }

    /// <summary>Fecha del último login exitoso (UTC).</summary>
    public DateTime? UltimoLogin { get; set; }

    /// <summary>Indica si la cuenta está activa. El admin puede desactivarla.</summary>
    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
