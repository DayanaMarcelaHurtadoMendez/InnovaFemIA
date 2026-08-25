namespace InnovaFemIA.Web.Features.Shared.Entities;

/// <summary>
/// Metadatos del boletín semanal y archivo de cotizaciones mayoristas SIPSA del DANE.
/// </summary>
public class BoletinSipsaInfo
{
    public int Id { get; set; }

    /// <summary>Periodo del boletín semanal (ej. "15 al 21 de agosto de 2026").</summary>
    public string Periodo { get; set; } = string.Empty;

    /// <summary>Enlace directo al documento PDF del Boletín Técnico.</summary>
    public string? BoletinTecnicoUrl { get; set; }

    /// <summary>Texto descriptivo o título del botón del Boletín Técnico.</summary>
    public string? BoletinTecnicoTitulo { get; set; }

    /// <summary>Enlace directo al archivo de cálculo Excel (.xlsx) con los anexos de precios.</summary>
    public string? AnexoExcelUrl { get; set; }

    /// <summary>Texto descriptivo del enlace de Anexo de Excel.</summary>
    public string? AnexoExcelTitulo { get; set; }

    /// <summary>Fecha de emisión o publicación del boletín por parte del DANE.</summary>
    public DateTime FechaPublicacion { get; set; } = DateTime.UtcNow;

    /// <summary>Fecha y hora en que el sistema ejecutó la sincronización con éxito (UTC).</summary>
    public DateTime FechaSincronizacion { get; set; } = DateTime.UtcNow;

    /// <summary>Cantidad de registros o precios mayoristas extraídos en este boletín.</summary>
    public int TotalRegistros { get; set; }

    /// <summary>Indica si el proceso de extracción de este boletín concluyó satisfactoriamente.</summary>
    public bool EsExitoso { get; set; } = true;
}
