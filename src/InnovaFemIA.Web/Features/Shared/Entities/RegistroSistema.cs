namespace InnovaFemIA.Web.Features.Shared.Entities;

/// <summary>
/// Log de eventos del sistema (errores de scraping, logins fallidos, errores de conexión, etc.).
/// Consultable desde el panel de administración de los 3 módulos.
/// </summary>
public class RegistroSistema
{
    public int Id { get; set; }

    /// <summary>
    /// Tipo de evento para filtrado en el panel de admin.
    /// Valores recomendados: "Error", "Advertencia", "Info", "LoginFallido", "ScrapingError".
    /// </summary>
    public string TipoEvento { get; set; } = string.Empty;

    /// <summary>Descripción detallada del evento o error.</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>
    /// Módulo del sistema que generó el evento.
    /// Valores: "Mapa", "Chatbot", "Biocomercio", "Sistema", "Scraping", "ApiKey".
    /// </summary>
    public string Modulo { get; set; } = string.Empty;

    /// <summary>Stack trace del error (solo para TipoEvento = "Error").</summary>
    public string? StackTrace { get; set; }

    /// <summary>IP de origen si aplica (ej. login fallido, request de API).</summary>
    public string? IpOrigen { get; set; }

    /// <summary>Fecha y hora del evento (UTC).</summary>
    public DateTime FechaHoraUtc { get; set; } = DateTime.UtcNow;
}
