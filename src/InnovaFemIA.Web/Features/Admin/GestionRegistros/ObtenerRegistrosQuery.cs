using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Admin.GestionRegistros;

public record RegistroLogDto(
    int Id,
    string TipoEvento,
    string Modulo,
    string Descripcion,
    string? StackTrace,
    string? IpOrigen,
    DateTime FechaHoraUtc
);

public record ObtenerRegistrosQuery(string? Modulo = null, string? TipoEvento = null) : IRequest<Result<List<RegistroLogDto>>>;

public class ObtenerRegistrosHandler : IRequestHandler<ObtenerRegistrosQuery, Result<List<RegistroLogDto>>>
{
    private readonly AppDbContext _db;

    public ObtenerRegistrosHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<RegistroLogDto>>> Handle(ObtenerRegistrosQuery request, CancellationToken cancellationToken)
    {
        var query = _db.RegistrosSistema.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Modulo))
        {
            query = query.Where(r => r.Modulo == request.Modulo);
        }

        if (!string.IsNullOrWhiteSpace(request.TipoEvento))
        {
            query = query.Where(r => r.TipoEvento == request.TipoEvento);
        }

        var logs = await query
            .OrderByDescending(r => r.FechaHoraUtc)
            .Take(100)
            .Select(r => new RegistroLogDto(
                r.Id,
                r.TipoEvento,
                r.Modulo,
                r.Descripcion,
                r.StackTrace,
                r.IpOrigen,
                r.FechaHoraUtc
            ))
            .ToListAsync(cancellationToken);

        return Result<List<RegistroLogDto>>.Success(logs);
    }
}
