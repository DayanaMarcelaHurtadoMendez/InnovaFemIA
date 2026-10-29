using InnovaFemIA.Web.Features.Shared.Common;
using MediatR;

namespace InnovaFemIA.Web.Features.Mapa.ObtenerCultivos;

/// <summary>
/// Query para obtener el catálogo de cultivos activos.
/// </summary>
public record ObtenerCultivosQuery() : IRequest<Result<IEnumerable<CultivoDto>>>;
