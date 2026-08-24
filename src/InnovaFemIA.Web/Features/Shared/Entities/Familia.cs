namespace InnovaFemIA.Web.Features.Shared.Entities;

/// <summary>
/// Representa una familia campesina de ASCAMPESINA registrada en el sistema.
/// Sus datos son visibles en el mapa (Módulo 1) y en el directorio de familias.
/// Su stock de productos es actualizado desde el Módulo 3 (Biocomercio).
/// </summary>
public class Familia
{
    public int Id { get; set; }

    /// <summary>Nombre de la familia (ej. "Familia González").</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Vereda a la que pertenece (ej. "El Pital", "Pescador").</summary>
    public string Vereda { get; set; } = string.Empty;

    /// <summary>Municipio (siempre "Caldono" en el contexto del proyecto).</summary>
    public string Municipio { get; set; } = "Caldono";

    /// <summary>Nombre de la persona de contacto principal.</summary>
    public string NombreContacto { get; set; } = string.Empty;

    /// <summary>Teléfono/WhatsApp de contacto.</summary>
    public string TelefonoContacto { get; set; } = string.Empty;

    /// <summary>Latitud geográfica para el pin del mapa Leaflet.</summary>
    public double Latitud { get; set; }

    /// <summary>Longitud geográfica para el pin del mapa Leaflet.</summary>
    public double Longitud { get; set; }

    /// <summary>
    /// Descripción de la familia campesina.
    /// </summary>
    public string? Descripcion { get; set; }

    /// <summary>
    /// Foto de la familia almacenada como bytes (LONGBLOB en MySQL).
    /// Se sirve como base64 en los DTOs al frontend.
    /// Null si no se ha subido foto.
    /// </summary>
    public byte[]? FotoBytes { get; set; }

    /// <summary>Tipo MIME de la foto (ej. "image/jpeg", "image/png").</summary>
    public string? FotoMimeType { get; set; }

    /// <summary>Indica si la familia está activa y visible en el mapa/directorio.</summary>
    public bool Activo { get; set; } = true;

    /// <summary>Fecha de registro en el sistema (UTC).</summary>
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navegación
    public ICollection<FamiliaCultivo> FamiliaCultivos { get; set; } = [];
    public ICollection<Stock> Stocks { get; set; } = [];
    public UsuarioFamilia? UsuarioFamilia { get; set; }
}
