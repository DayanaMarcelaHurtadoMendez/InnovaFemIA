namespace InnovaFemIA.Web.Features.Shared.Entities;

/// <summary>
/// Contenido educativo del Chatbot (Módulo 2).
/// Cargado por el equipo investigador desde el panel de administración.
/// Se sirve al usuario según la combinación Cultivo + Tema.
/// </summary>
public class ContenidoChatbot
{
    public int Id { get; set; }

    public int CultivoId { get; set; }
    public Cultivo Cultivo { get; set; } = null!;

    /// <summary>
    /// Tema del contenido (ej. "CuandoSembrar", "CuandoCosechar", "PlagesEnfermedades", "Abonado").
    /// Se usa para el flujo de menú en cascada del chatbot.
    /// </summary>
    public TemaContenido Tema { get; set; }

    /// <summary>Tipo de recurso educativo.</summary>
    public TipoRecurso TipoRecurso { get; set; }

    /// <summary>Título descriptivo del contenido.</summary>
    public string Titulo { get; set; } = string.Empty;

    /// <summary>
    /// URL del video/infografía/documento (YouTube embed, Drive, etc.).
    /// Null si el archivo se almacena directamente en ArchivoBytes.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Archivo almacenado directamente en la base de datos como LONGBLOB.
    /// Se usa para documentos PDF o infografías subidas desde el panel admin.
    /// Null si se usa Url.
    /// </summary>
    public byte[]? ArchivoBytes { get; set; }

    public string? ArchivoMimeType { get; set; }

    public string? ArchivoNombre { get; set; }

    /// <summary>
    /// Orden de presentación cuando hay varios recursos para la misma combinación Cultivo+Tema.
    /// El chatbot los sirve en este orden cuando el usuario pide "Ver más".
    /// </summary>
    public int Orden { get; set; } = 1;

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

/// <summary>Temas disponibles en el flujo del chatbot por menú.</summary>
public enum TemaContenido
{
    CuandoSembrar = 1,
    CuandoCosechar = 2,
    PlagasEnfermedades = 3,
    Abonado = 4,
    PodaMantenimiento = 5,
    RiegoDrenaje = 6,
    PostcosechaManejo = 7,
    ManejoAgroecologico = 8,
    ComercializacionCalidad = 9
}

/// <summary>Tipos de recurso educativo que puede cargar el equipo investigador.</summary>
public enum TipoRecurso
{
    Video = 1,
    Infografia = 2,
    Documento = 3
}
