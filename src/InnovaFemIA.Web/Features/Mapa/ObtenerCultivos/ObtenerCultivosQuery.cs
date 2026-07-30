using InnovaFemIA.Web.Features.Shared.Common;
using MediatR;

namespace InnovaFemIA.Web.Features.Mapa.ObtenerCultivos;

/// <summary>
/// Query para obtener el catálogo de cultivos activos.
/// </summary>
public record ObtenerCultivosQuery() : IRequest<Result<IEnumerable<CultivoDto>>>;

public class CultivoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Icono { get; set; } = "🌱";
}
