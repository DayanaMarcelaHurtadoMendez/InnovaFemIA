namespace InnovaFemIA.Web.Features.Shared.Entities;

/// <summary>
/// Stock de disponibilidad de productos por familia.
/// Es el punto de integración clave entre Módulo 3 (Biocomercio - las familias actualizan)
/// y Módulo 1 (Mapa/Directorio - los visitantes consultan).
/// </summary>
public class Stock
{
    public int Id { get; set; }

    public int FamiliaId { get; set; }
    public Familia Familia { get; set; } = null!;

    public int CultivoId { get; set; }
    public Cultivo Cultivo { get; set; } = null!;

    /// <summary>
    /// true = la familia tiene este producto disponible para venta hoy.
    /// false = no disponible (se muestra tachado en el directorio).
    /// </summary>
    public bool Disponible { get; set; } = false;

    /// <summary>Última vez que la familia actualizó la disponibilidad de este producto (UTC).</summary>
    public DateTime UltimaActualizacion { get; set; } = DateTime.UtcNow;

    /// <summary>Notas adicionales opcionales (ej. "Disponible desde la próxima semana").</summary>
    public string? Notas { get; set; }
}
