using InnovaFemIA.Web.Features.Shared.Common;
using MediatR;

namespace InnovaFemIA.Web.Features.Biocomercio.ObtenerPrecios;

/// <summary>
/// Query para obtener los precios mayoristas SIPSA DANE y los precios de referencia de cultivos para el portal de Biocomercio.
/// </summary>
public record ObtenerPreciosQuery(
    string? Grupo = null,
    string? Busqueda = null,
    string? Mercado = null
) : IRequest<Result<PreciosSipsaViewModel>>;

public class PreciosSipsaViewModel
{
    public BoletinSipsaInfoDto? BoletinActual { get; set; }
    public IEnumerable<PrecioMercadoDto> PreciosCultivos { get; set; } = Enumerable.Empty<PrecioMercadoDto>();
    public IEnumerable<PrecioMayoristaSipsaDto> CotizacionesSipsa { get; set; } = Enumerable.Empty<PrecioMayoristaSipsaDto>();
    public IEnumerable<string> GruposDisponibles { get; set; } = Enumerable.Empty<string>();
    public IEnumerable<string> MercadosDisponibles { get; set; } = Enumerable.Empty<string>();
    public int TotalRegistros { get; set; }
    public DateTime UltimaActualizacion { get; set; } = DateTime.UtcNow;
}

public class BoletinSipsaInfoDto
{
    public string Periodo { get; set; } = string.Empty;
    public string? BoletinTecnicoUrl { get; set; }
    public string? BoletinTecnicoTitulo { get; set; }
    public string? AnexoExcelUrl { get; set; }
    public string? AnexoExcelTitulo { get; set; }
    public DateTime FechaPublicacion { get; set; }
    public DateTime FechaSincronizacion { get; set; }
    public bool EsReciente { get; set; }
}

public class PrecioMayoristaSipsaDto
{
    public int Id { get; set; }
    public string GrupoAlimento { get; set; } = string.Empty;
    public string Producto { get; set; } = string.Empty;
    public string MercadoMayorista { get; set; } = string.Empty;
    public decimal PrecioMinimo { get; set; }
    public decimal PrecioMaximo { get; set; }
    public decimal PrecioMedio { get; set; }
    public string Tendencia { get; set; } = "=";
    public string PeriodoBoletin { get; set; } = string.Empty;
    public DateTime FechaActualizacion { get; set; }
    public int? CultivoId { get; set; }
}

public class PrecioMercadoDto
{
    public int Id { get; set; }
    public int CultivoId { get; set; }
    public string NombreCultivo { get; set; } = string.Empty;
    public string IconoCultivo { get; set; } = "🌱";
    public decimal Precio { get; set; }
    public decimal? PrecioAnterior { get; set; }
    public decimal PrecioMinimo { get; set; }
    public decimal PrecioMaximo { get; set; }
    public string MercadoReferencia { get; set; } = string.Empty;
    public string Tendencia { get; set; } = "=";
    public string EstadoVariacion { get; set; } = "Igual";
    public decimal PorcentajeVariacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public string Fuente { get; set; } = "DANE";
    public bool EsActualizado { get; set; }
}
