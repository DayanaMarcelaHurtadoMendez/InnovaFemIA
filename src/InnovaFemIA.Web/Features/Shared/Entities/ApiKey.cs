namespace InnovaFemIA.Web.Features.Shared.Entities;

/// <summary>
/// API Key para acceso externo a endpoints marcados con [PublicApi].
/// El middleware ApiKeyMiddleware valida cada request contra esta tabla.
/// </summary>
public class ApiKey
{
    public int Id { get; set; }

    /// <summary>
    /// Clave secreta en formato GUID hasheado (SHA-256).
    /// El cliente envía la clave plana en X-Api-Key; el sistema hashea y compara.
    /// </summary>
    public string Clave { get; set; } = string.Empty;

    /// <summary>Nombre descriptivo de la aplicación o integración que usa esta clave.</summary>
    public string NombreApp { get; set; } = string.Empty;

    /// <summary>Descripción opcional del uso autorizado de esta clave.</summary>
    public string? Descripcion { get; set; }

    /// <summary>Indica si la clave está activa. El admin puede revocarla sin eliminarla.</summary>
    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    /// <summary>Último uso registrado de la clave (UTC). Actualizado por el middleware.</summary>
    public DateTime? UltimoUso { get; set; }

    /// <summary>
    /// IP de origen del último uso (para auditoría).
    /// </summary>
    public string? UltimaIpUso { get; set; }
}
