using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Entities;
using InnovaFemIA.Web.Features.Shared.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Mapa.ObtenerActores;

/// <summary>
/// Handler para consultar los actores adicionales del mapa (asociaciones y puntos de compra).
/// </summary>
public class ObtenerActoresHandler : IRequestHandler<ObtenerActoresQuery, Result<IEnumerable<ActorDto>>>
{
    private readonly AppDbContext _db;

    public ObtenerActoresHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<IEnumerable<ActorDto>>> Handle(
        ObtenerActoresQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.ActoresMapa
            .AsNoTracking()
            .Where(a => a.Activo);

        if (!string.IsNullOrWhiteSpace(request.Tipo) && Enum.TryParse<TipoActorMapa>(request.Tipo, true, out var tipoEnum))
        {
            query = query.Where(a => a.TipoActor == tipoEnum);
        }

        var actores = await query
            .OrderBy(a => a.Nombre)
            .ToListAsync(cancellationToken);

        var dtos = actores.Select(a => new ActorDto
        {
            Id = a.Id,
            Nombre = a.Nombre,
            TipoActor = a.TipoActor.ToString(),
            Descripcion = a.Descripcion,
            DatosContacto = a.DatosContacto,
            Latitud = a.Latitud,
            Longitud = a.Longitud,
            LogoBase64 = a.LogoBytes is not null && a.LogoMimeType is not null
                ? $"data:{a.LogoMimeType};base64,{Convert.ToBase64String(a.LogoBytes)}"
                : null
        });

        return Result<IEnumerable<ActorDto>>.Success(dtos);
    }
}
