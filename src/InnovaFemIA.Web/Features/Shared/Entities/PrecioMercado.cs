namespace InnovaFemIA.Web.Features.Shared.Entities;

/// <summary>
/// Precio de mercado de un producto agrícola obtenido mediante web scraping del DANE.
/// Compartido entre Módulo 1 (pantalla de precios pública) y Módulo 3 (Biocomercio con variación).
/// </summary>
public class PrecioMercado
{
    public int Id { get; set; }

    public int CultivoId { get; set; }
    public Cultivo Cultivo { get; set; } = null!;

    /// <summary>Precio en pesos colombianos por kilogramo.</summary>
    public decimal Precio { get; set; }

    /// <summary>
    /// Precio del día anterior, usado en Módulo 3 para calcular
    /// la variación (sube/baja/igual) con indicador visual de color.
    /// Null si es el primer registro para este cultivo.
    /// </summary>
    public decimal? PrecioAnterior { get; set; }

    /// <summary>Fecha y hora de obtención del precio (UTC).</summary>
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fuente del precio (ej. "DANE - Sipsa").
    /// Permite auditar la procedencia del dato.
    /// </summary>
    public string Fuente { get; set; } = "DANE";

    /// <summary>
    /// Indica si el scraping fue exitoso o si se muestra el último precio conocido
    /// con advertencia de posible desactualización.
    /// </summary>
    public bool EsActualizado { get; set; } = true;
}
