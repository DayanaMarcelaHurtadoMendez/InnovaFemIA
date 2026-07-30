using InnovaFemIA.Web.Features.Shared.Common;
using MediatR;

namespace InnovaFemIA.Web.Features.Mapa.ObtenerActores;

/// <summary>
/// Query para obtener los actores del mapa (asociaciones campesinas y puntos de compra).
/// </summary>
public record ObtenerActoresQuery(string? Tipo = null) : IRequest<Result<IEnumerable<ActorDto>>>;
