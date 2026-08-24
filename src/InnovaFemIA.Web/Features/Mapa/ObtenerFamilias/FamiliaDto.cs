namespace InnovaFemIA.Web.Features.Mapa.ObtenerFamilias;

/// <summary>
/// DTO de familia para el directorio (Módulo 1).
/// La foto se expone como Base64 para ser incrustada en el HTML como data URI.
/// </summary>
public class FamiliaDto
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Vereda { get; init; } = string.Empty;
    public string Municipio { get; init; } = string.Empty;
    public string NombreContacto { get; init; } = string.Empty;
    public string TelefonoContacto { get; init; } = string.Empty;
    public double Latitud { get; init; }
    public double Longitud { get; init; }

    /// <summary>Descripción de la familia campesina.</summary>
    public string? Descripcion { get; init; }

    /// <summary>
    /// Foto como string Base64 con prefijo data URI.
    /// Formato: "data:image/jpeg;base64,/9j/4AAQ..."
    /// Null si la familia no tiene foto asignada.
    /// </summary>
    public string? FotoBase64 { get; init; }

    /// <summary>Cultivos que produce esta familia, con su estado de stock actual.</summary>
    public IEnumerable<CultivStockDto> Cultivos { get; init; } = [];
}

/// <summary>Cultivo de una familia con indicador de disponibilidad actual.</summary>
public class CultivStockDto
{
    public int CultivoId { get; init; }
    public string NombreCultivo { get; init; } = string.Empty;
    public string Icono { get; init; } = string.Empty;

    /// <summary>true = disponible (verde), false = no disponible (tachado).</summary>
    public bool Disponible { get; init; }
}
