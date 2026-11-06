using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Entities;
using InnovaFemIA.Web.Features.Shared.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Admin.GestionTiemposCosecha;

// DTOs
public record CultivoTiempoAdminDto(
    int Id,
    int CultivoId,
    string NombreCultivo,
    ActividadAgricola Actividad,
    int MesesHastaCosecha,
    string DescripcionTemplate,
    string? Notas
);

// Queries & Commands
public record ObtenerTiemposCosechaAdminQuery : IRequest<Result<List<CultivoTiempoAdminDto>>>;

public record CrearTiempoCosechaCommand(
    int CultivoId,
    ActividadAgricola Actividad,
    int MesesHastaCosecha,
    string DescripcionTemplate,
    string? Notas
) : IRequest<Result<int>>;

public record EditarTiempoCosechaCommand(
    int Id,
    int CultivoId,
    ActividadAgricola Actividad,
    int MesesHastaCosecha,
    string DescripcionTemplate,
    string? Notas
) : IRequest<Result<bool>>;

public record EliminarTiempoCosechaCommand(int Id) : IRequest<Result<bool>>;

// Handlers
public class GestionTiemposHandler :
    IRequestHandler<ObtenerTiemposCosechaAdminQuery, Result<List<CultivoTiempoAdminDto>>>,
    IRequestHandler<CrearTiempoCosechaCommand, Result<int>>,
    IRequestHandler<EditarTiempoCosechaCommand, Result<bool>>,
    IRequestHandler<EliminarTiempoCosechaCommand, Result<bool>>
{
    private readonly AppDbContext _db;

    public GestionTiemposHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<CultivoTiempoAdminDto>>> Handle(ObtenerTiemposCosechaAdminQuery request, CancellationToken cancellationToken)
    {
        var tiempos = await _db.CultivosTiempos
            .Include(ct => ct.Cultivo)
            .OrderBy(ct => ct.Cultivo.Nombre)
            .ThenBy(ct => ct.Actividad)
            .Select(ct => new CultivoTiempoAdminDto(
                ct.Id,
                ct.CultivoId,
                ct.Cultivo != null ? ct.Cultivo.Nombre : "Desconocido",
                ct.Actividad,
                ct.MesesHastaCosecha,
                ct.DescripcionTemplate,
                ct.Notas
            ))
            .ToListAsync(cancellationToken);

        return Result<List<CultivoTiempoAdminDto>>.Success(tiempos);
    }

    public async Task<Result<int>> Handle(CrearTiempoCosechaCommand request, CancellationToken cancellationToken)
    {
        if (request.MesesHastaCosecha <= 0)
            return Result<int>.BadRequest("El número de meses hasta cosecha debe ser mayor a 0.");

        var tiempo = new CultivoTiempo
        {
            CultivoId = request.CultivoId,
            Actividad = request.Actividad,
            MesesHastaCosecha = request.MesesHastaCosecha,
            DescripcionTemplate = string.IsNullOrWhiteSpace(request.DescripcionTemplate) ? "Tu cultivo estará listo para cosecha en aproximadamente {meses} meses" : request.DescripcionTemplate.Trim(),
            Notas = request.Notas?.Trim()
        };

        _db.CultivosTiempos.Add(tiempo);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(tiempo.Id);
    }

    public async Task<Result<bool>> Handle(EditarTiempoCosechaCommand request, CancellationToken cancellationToken)
    {
        var tiempo = await _db.CultivosTiempos.FirstOrDefaultAsync(ct => ct.Id == request.Id, cancellationToken);
        if (tiempo == null) return Result<bool>.NotFound("Tiempo de cosecha no encontrado.");

        tiempo.CultivoId = request.CultivoId;
        tiempo.Actividad = request.Actividad;
        tiempo.MesesHastaCosecha = request.MesesHastaCosecha;
        tiempo.DescripcionTemplate = request.DescripcionTemplate.Trim();
        tiempo.Notas = request.Notas?.Trim();

        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> Handle(EliminarTiempoCosechaCommand request, CancellationToken cancellationToken)
    {
        var tiempo = await _db.CultivosTiempos.FirstOrDefaultAsync(ct => ct.Id == request.Id, cancellationToken);
        if (tiempo == null) return Result<bool>.NotFound("Tiempo de cosecha no encontrado.");

        _db.CultivosTiempos.Remove(tiempo);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
