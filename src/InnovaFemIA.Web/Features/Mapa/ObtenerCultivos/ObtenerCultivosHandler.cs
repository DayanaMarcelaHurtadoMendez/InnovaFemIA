using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Mapa.ObtenerCultivos;

/// <summary>
/// Handler para consultar los cultivos activos del sistema.
/// </summary>
public class ObtenerCultivosHandler : IRequestHandler<ObtenerCultivosQuery, Result<IEnumerable<CultivoDto>>>
{
    private readonly AppDbContext _db;

    public ObtenerCultivosHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<IEnumerable<CultivoDto>>> Handle(
        ObtenerCultivosQuery request,
        CancellationToken cancellationToken)
    {
        var cultivos = await _db.Cultivos
            .AsNoTracking()
            .Where(c => c.Activo)
            .OrderBy(c => c.Nombre)
            .ToListAsync(cancellationToken);

        var dtos = cultivos.Select(c => new CultivoDto
        {
            Id = c.Id,
            Nombre = c.Nombre,
            Descripcion = c.Descripcion,
            Icono = c.Icono
        });

        return Result<IEnumerable<CultivoDto>>.Success(dtos);
    }
}
