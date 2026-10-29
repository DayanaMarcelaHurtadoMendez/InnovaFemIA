using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Admin.AdminDashboard;

public record AdminDashboardDto(
    int TotalContenidos,
    int TotalLecciones,
    int TotalPreguntas,
    int TotalCultivos,
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
        var totalContenidos = await _db.ContenidosChatbot.CountAsync(cancellationToken);
        var totalLecciones = await _db.LeccionesChatbot.CountAsync(cancellationToken);
        var totalPreguntas = await _db.PreguntasLecciones.CountAsync(cancellationToken);
        var totalCultivos = await _db.Cultivos.CountAsync(cancellationToken);
        var totalErrores = await _db.RegistrosSistema.CountAsync(r => r.TipoEvento.Contains("Error"), cancellationToken);

        var ultimosErrores = await _db.RegistrosSistema
            .Where(r => r.TipoEvento.Contains("Error"))
            .OrderByDescending(r => r.FechaHoraUtc)
            .Take(5)
            .Select(r => new UltimoLogErrorDto(r.Id, r.Modulo, r.TipoEvento, r.Descripcion, r.FechaHoraUtc))
            .ToListAsync(cancellationToken);

        return Result<AdminDashboardDto>.Success(new AdminDashboardDto(
            totalContenidos,
            totalLecciones,
            totalPreguntas,
            totalCultivos,
            totalErrores,
            ultimosErrores
        ));
    }
}
