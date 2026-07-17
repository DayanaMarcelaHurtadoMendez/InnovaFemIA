using InnovaFemIA.Web.Features.Shared.Common;
using MediatR;

namespace InnovaFemIA.Web.Features.Mapa.ObtenerFamilias;

/// <summary>
/// Query para obtener el listado de familias del directorio (Módulo 1, Pantalla 1).
/// Incluye filtros por nombre, producto y disponibilidad.
/// Usada tanto por la vista Razor como por el MapaApiController.
/// </summary>
public record ObtenerFamiliasQuery(
    string? Busqueda = null,
    int? CultivoId = null,
    bool SoloActivas = true
) : IRequest<Result<IEnumerable<FamiliaDto>>>;
