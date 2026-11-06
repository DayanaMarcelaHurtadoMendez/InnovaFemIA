using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Admin.AdminDashboard;

public record AdminDashboardDto(
    int TotalFamilias,
    int TotalAsociaciones,
    int TotalPuntosCompra,
    int TotalCultivos,
    int TotalCuentasFamilias,
    int TotalReglasLunares,
    int TotalErroresLog,
    List<UltimoLogErrorDto> UltimosErrores
);

public record UltimoLogErrorDto(int Id, string Modulo, string TipoEvento, string Descripcion, DateTime FechaHoraUtc);

public record ObtenerDashboardQuery : IRequest<Result<AdminDashboardDto>>;

public class ObtenerDashboardHandler : IRequestHandler<ObtenerDashboardQuery, Result<AdminDashboardDto>>
{
    private readonly AppDbContext _db;

    public ObtenerDashboardHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<AdminDashboardDto>> Handle(ObtenerDashboardQuery request, CancellationToken cancellationToken)
    {
        var totalFamilias = await _db.Familias.CountAsync(cancellationToken);
        var totalAsociaciones = await _db.ActoresMapa.CountAsync(a => a.TipoActor == Shared.Entities.TipoActorMapa.Asociacion, cancellationToken);
        var totalPuntosCompra = await _db.ActoresMapa.CountAsync(a => a.TipoActor == Shared.Entities.TipoActorMapa.PuntoCompra, cancellationToken);
        var totalCultivos = await _db.Cultivos.CountAsync(cancellationToken);
        var totalCuentas = await _db.UsuariosFamilias.CountAsync(cancellationToken);
        var totalReglasLunares = await _db.ReglasLunares.CountAsync(cancellationToken);
        var totalErrores = await _db.RegistrosSistema.CountAsync(r => r.TipoEvento.Contains("Error"), cancellationToken);

        var ultimosErrores = await _db.RegistrosSistema
            .Where(r => r.TipoEvento.Contains("Error"))
            .OrderByDescending(r => r.FechaHoraUtc)
            .Take(5)
            .Select(r => new UltimoLogErrorDto(r.Id, r.Modulo, r.TipoEvento, r.Descripcion, r.FechaHoraUtc))
            .ToListAsync(cancellationToken);

        return Result<AdminDashboardDto>.Success(new AdminDashboardDto(
            totalFamilias,
            totalAsociaciones,
            totalPuntosCompra,
            totalCultivos,
            totalCuentas,
            totalReglasLunares,
            totalErrores,
            ultimosErrores
        ));
    }
}
