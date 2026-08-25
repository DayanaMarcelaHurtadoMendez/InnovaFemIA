namespace InnovaFemIA.Web.Features.Shared.Entities;

/// <summary>
/// Cotización de precio mayorista obtenida del Sistema de Información de Precios
/// y Abastecimiento del Sector Agropecuario (SIPSA) del DANE.
/// </summary>
public class PrecioMayoristaSipsa
{
    public int Id { get; set; }

    /// <summary>
    /// Grupo o categoría del alimento según el boletín SIPSA:
    /// 1.1 Verduras y hortalizas, 1.2 Frutas frescas, 1.3 Tubérculos, raíces y plátanos,
    /// 1.4 Granos y cereales, 1.5 Huevos y lácteos, 1.6 Carnes, 1.7 Pescados, 1.8 Procesados.
    /// </summary>
    public string GrupoAlimento { get; set; } = string.Empty;

    /// <summary>Nombre del producto (ej: "Acelga", "Aguacate Hass", "Plátano hartón verde").</summary>
    public string Producto { get; set; } = string.Empty;

    /// <summary>Mercado o central mayorista (ej: "Cali, Cavasa", "Popayán, Plaza", "Bogotá, Corabastos").</summary>
    public string MercadoMayorista { get; set; } = string.Empty;

    /// <summary>Precio mínimo en pesos colombianos por kilogramo ($/Kg).</summary>
    public decimal PrecioMinimo { get; set; }

    /// <summary>Precio máximo en pesos colombianos por kilogramo ($/Kg).</summary>
    public decimal PrecioMaximo { get; set; }

    /// <summary>Precio promedio o medio en pesos colombianos por kilogramo ($/Kg).</summary>
    public decimal PrecioMedio { get; set; }

    /// <summary>
    /// Código oficial de tendencia SIPSA:
    /// '---' (&lt; -20%), '--' (-10% a -20%), '-' (-0.01% a -10%), '=' (0%),
    /// '+' (0.01% a 10%), '++' (10% a 20%), '+++' (&gt; 20%), 'n.d' (no disponible).
    /// </summary>
    public string Tendencia { get; set; } = "=";

    /// <summary>Periodo del boletín semanal (ej: "15 al 21 de agosto de 2026").</summary>
    public string PeriodoBoletin { get; set; } = string.Empty;

    /// <summary>Fecha del boletín.</summary>
    public DateTime FechaBoletin { get; set; } = DateTime.UtcNow;

    /// <summary>Fecha y hora en que se sincronizó el registro en el sistema (UTC).</summary>
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Relación opcional con un cultivo del sistema si coincide por nombre.
    /// </summary>
    public int? CultivoId { get; set; }
    public Cultivo? Cultivo { get; set; }
}
