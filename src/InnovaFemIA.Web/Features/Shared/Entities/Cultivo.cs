namespace InnovaFemIA.Web.Features.Shared.Entities;

/// <summary>
/// Cultivo agrícola registrado en el sistema.
/// Entidad central consultada en el Módulo 2 (Chatbot / Asistente Asalia).
/// </summary>
public class Cultivo
{
    public int Id { get; set; }

    /// <summary>Nombre del cultivo (ej. "Café", "Aguacate", "Plátano").</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Descripción breve del cultivo para el chatbot.</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>
    /// Ícono representativo del cultivo en emoji o nombre de ícono
    /// (ej. "☕", "🥑", "🍌"). Usado en el menú de selección de Asalia.
    /// </summary>
    public string Icono { get; set; } = "🌱";

    /// <summary>Activo indica si el cultivo está disponible en los selectores del sistema.</summary>
    public bool Activo { get; set; } = true;

    // Navegación
    public ICollection<ContenidoChatbot> ContenidosChatbot { get; set; } = [];
}
