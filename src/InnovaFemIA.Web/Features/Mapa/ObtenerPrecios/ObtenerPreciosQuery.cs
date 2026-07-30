using InnovaFemIA.Web.Features.Shared.Common;
using MediatR;

namespace InnovaFemIA.Web.Features.Mapa.ObtenerPrecios;

/// <summary>
/// Query para obtener los precios de mercado DANE vigentes.
/// </summary>
public record ObtenerPreciosQuery() : IRequest<Result<IEnumerable<PrecioMercadoDto>>>;

public class PrecioMercadoDto
{
    public int Id { get; set; }
    public int CultivoId { get; set; }
    public string NombreCultivo { get; set; } = string.Empty;
    public string IconoCultivo { get; set; } = "🌱";
    public decimal Precio { get; set; }
    public decimal? PrecioAnterior { get; set; }
    public string EstadoVariacion { get; set; } = "Igual"; // "Sube", "Baja", "Igual"
    public decimal PorcentajeVariacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public string Fuente { get; set; } = "DANE";
    public bool EsActualizado { get; set; }
}
