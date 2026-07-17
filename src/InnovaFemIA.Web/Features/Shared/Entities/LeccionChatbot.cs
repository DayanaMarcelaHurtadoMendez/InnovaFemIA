namespace InnovaFemIA.Web.Features.Shared.Entities;

/// <summary>
/// Lección educativa del modo Duolingo del Chatbot (Módulo 2).
/// El progreso del usuario se guarda en localStorage del navegador (sin login).
/// </summary>
public class LeccionChatbot
{
    public int Id { get; set; }

    public string Titulo { get; set; } = string.Empty;

    /// <summary>Descripción breve de la lección (se muestra antes de iniciar).</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>
    /// Imagen de portada de la lección almacenada como LONGBLOB.
    /// Null si no se ha asignado imagen.
    /// </summary>
    public byte[]? ImagenBytes { get; set; }

    public string? ImagenMimeType { get; set; }

    /// <summary>Número de lección para determinar el orden de secuencia.</summary>
    public int NumeroLeccion { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navegación
    public ICollection<PreguntaLeccion> Preguntas { get; set; } = [];
}

/// <summary>
/// Pregunta de selección múltiple dentro de una lección Duolingo.
/// </summary>
public class PreguntaLeccion
{
    public int Id { get; set; }

    public int LeccionId { get; set; }
    public LeccionChatbot Leccion { get; set; } = null!;

    /// <summary>Texto de la pregunta.</summary>
    public string TextoPregunta { get; set; } = string.Empty;

    /// <summary>
    /// Contenido explicativo breve (imagen + texto) mostrado antes de la pregunta.
    /// </summary>
    public string? ContenidoExplicativo { get; set; }

    /// <summary>Imagen ilustrativa del contenido explicativo (LONGBLOB, nullable).</summary>
    public byte[]? ImagenExplicativaBytes { get; set; }

    public string? ImagenExplicativaMimeType { get; set; }

    /// <summary>Orden de la pregunta dentro de la lección.</summary>
    public int Orden { get; set; }

    // Navegación
    public ICollection<OpcionPregunta> Opciones { get; set; } = [];
}

/// <summary>
/// Opción de respuesta para una pregunta de selección múltiple.
/// Siempre hay 3 opciones por pregunta (según especificación del MD).
/// </summary>
public class OpcionPregunta
{
    public int Id { get; set; }

    public int PreguntaId { get; set; }
    public PreguntaLeccion Pregunta { get; set; } = null!;

    public string TextoOpcion { get; set; } = string.Empty;

    /// <summary>Indica si esta es la opción correcta.</summary>
    public bool EsCorrecta { get; set; } = false;

    /// <summary>
    /// Retroalimentación mostrada al usuario cuando selecciona esta opción.
    /// Ej: "¡Correcto! El café tarda entre 8 y 10 meses..."
    /// </summary>
    public string? Retroalimentacion { get; set; }
}
