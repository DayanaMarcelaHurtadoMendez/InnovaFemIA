namespace InnovaFemIA.Web.Features.Shared.Entities;

/// <summary>
/// Actor adicional del mapa interactivo (Módulo 1):
/// puede ser una asociación campesina (ícono naranja) o un punto de compra (ícono azul).
/// Las familias ASCAMPESINA (ícono verde) se mapean desde la entidad Familia.
/// </summary>
public class ActorMapa
{
    public int Id { get; set; }

    /// <summary>Nombre del actor (ej. "Asociación Mujeres Caldono").</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de actor: "Asociacion" (naranja) o "PuntoCompra" (azul).
    /// Se usa para determinar el ícono del marcador en Leaflet.
    /// </summary>
    public TipoActorMapa TipoActor { get; set; }

    /// <summary>Descripción del actor mostrada en el pop-up del mapa.</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Datos de contacto (teléfono, dirección, etc.).</summary>
    public string DatosContacto { get; set; } = string.Empty;

    /// <summary>Latitud para el marcador en Leaflet.</summary>
    public double Latitud { get; set; }

    /// <summary>Longitud para el marcador en Leaflet.</summary>
    public double Longitud { get; set; }

    /// <summary>
    /// Logo o foto del actor almacenada como bytes (LONGBLOB en MySQL).
    /// Nullable si no se ha subido imagen.
    /// </summary>
    public byte[]? LogoBytes { get; set; }

    public string? LogoMimeType { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

/// <summary>Tipos de actores representados en el mapa.</summary>
public enum TipoActorMapa
{
    /// <summary>Otras asociaciones campesinas de la región (ícono naranja).</summary>
    Asociacion = 1,

    /// <summary>Puntos de compra de productos agrícolas (ícono azul).</summary>
    PuntoCompra = 2
}
