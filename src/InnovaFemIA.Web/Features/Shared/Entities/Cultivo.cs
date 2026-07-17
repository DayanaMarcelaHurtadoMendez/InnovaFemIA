namespace InnovaFemIA.Web.Features.Shared.Entities;

/// <summary>
/// Cultivo agrícola registrado en el sistema.
/// Es la entidad central compartida entre Módulo 1 (Mapa), Módulo 2 (Chatbot) y Módulo 3 (Biocomercio).
/// </summary>
public class Cultivo
{
    public int Id { get; set; }

    /// <summary>Nombre del cultivo (ej. "Café", "Aguacate", "Plátano").</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Descripción breve del cultivo para el chatbot y directorio.</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>
    /// Ícono representativo del cultivo en emoji o nombre de ícono
    /// (ej. "☕", "🥑", "🍌"). Usado en tarjetas del directorio y precios.
    /// </summary>
    public string Icono { get; set; } = "🌱";

    /// <summary>Activo indica si el cultivo está disponible en los selectores del sistema.</summary>
    public bool Activo { get; set; } = true;

    // Navegación
    public ICollection<FamiliaCultivo> FamiliaCultivos { get; set; } = [];
    public ICollection<Stock> Stocks { get; set; } = [];
    public ICollection<PrecioMercado> PreciosMercado { get; set; } = [];
    public ICollection<ReglasLunares> ReglasLunares { get; set; } = [];
    public ICollection<CultivoTiempo> CultivosTiempos { get; set; } = [];
    public ICollection<ContenidoChatbot> ContenidosChatbot { get; set; } = [];
}
