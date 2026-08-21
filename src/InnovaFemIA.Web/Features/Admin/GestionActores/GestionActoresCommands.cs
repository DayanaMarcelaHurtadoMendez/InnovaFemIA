using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Entities;
using InnovaFemIA.Web.Features.Shared.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Admin.GestionActores;

// DTOs
public record ActorAdminDto(
    int Id,
    string Nombre,
    TipoActorMapa TipoActor,
    string Descripcion,
    string DatosContacto,
    double Latitud,
    double Longitud,
    bool Activo
);

// Queries & Commands
public record ObtenerActoresAdminQuery(TipoActorMapa? TipoActor = null) : IRequest<Result<List<ActorAdminDto>>>;

public record CrearActorAdminCommand(
    string Nombre,
    TipoActorMapa TipoActor,
    string Descripcion,
    string DatosContacto,
    double Latitud,
    double Longitud
) : IRequest<Result<int>>;

public record EditarActorAdminCommand(
    int Id,
    string Nombre,
    TipoActorMapa TipoActor,
    string Descripcion,
    string DatosContacto,
    double Latitud,
    double Longitud,
    bool Activo
) : IRequest<Result<bool>>;

public record EliminarActorAdminCommand(int Id) : IRequest<Result<bool>>;

// Handlers
public class GestionActoresHandler :
    IRequestHandler<ObtenerActoresAdminQuery, Result<List<ActorAdminDto>>>,
    IRequestHandler<CrearActorAdminCommand, Result<int>>,
    IRequestHandler<EditarActorAdminCommand, Result<bool>>,
    IRequestHandler<EliminarActorAdminCommand, Result<bool>>
{
    private readonly AppDbContext _db;

    public GestionActoresHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<ActorAdminDto>>> Handle(ObtenerActoresAdminQuery request, CancellationToken cancellationToken)
    {
        var query = _db.ActoresMapa.AsQueryable();

        if (request.TipoActor.HasValue)
        {
            query = query.Where(a => a.TipoActor == request.TipoActor.Value);
        }

        var actores = await query
            .OrderBy(a => a.Nombre)
            .Select(a => new ActorAdminDto(
                a.Id,
                a.Nombre,
                a.TipoActor,
                a.Descripcion ?? string.Empty,
                a.DatosContacto ?? string.Empty,
                a.Latitud,
                a.Longitud,
                a.Activo
            ))
            .ToListAsync(cancellationToken);

        return Result<List<ActorAdminDto>>.Success(actores);
    }

    public async Task<Result<int>> Handle(CrearActorAdminCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Length < 3 || request.Nombre.Length > 200)
            return Result<int>.BadRequest("El nombre del actor debe tener entre 3 y 200 caracteres.");

        if (request.Latitud < -4.23 || request.Latitud > 13.39 || request.Longitud < -79.00 || request.Longitud > -66.85)
            return Result<int>.BadRequest("Las coordenadas geográficas deben estar dentro del territorio de Colombia.");

        var actor = new ActorMapa
        {
            Nombre = request.Nombre.Trim(),
            TipoActor = request.TipoActor,
            Descripcion = request.Descripcion?.Trim() ?? string.Empty,
            DatosContacto = request.DatosContacto?.Trim() ?? string.Empty,
            Latitud = request.Latitud,
            Longitud = request.Longitud,
            Activo = true
        };

        _db.ActoresMapa.Add(actor);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(actor.Id);
    }

    public async Task<Result<bool>> Handle(EditarActorAdminCommand request, CancellationToken cancellationToken)
    {
        var actor = await _db.ActoresMapa.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (actor == null) return Result<bool>.NotFound("Actor no encontrado.");

        if (request.Latitud < -4.23 || request.Latitud > 13.39 || request.Longitud < -79.00 || request.Longitud > -66.85)
            return Result<bool>.BadRequest("Las coordenadas geográficas deben estar dentro del territorio de Colombia.");

        actor.Nombre = request.Nombre.Trim();
        actor.TipoActor = request.TipoActor;
        actor.Descripcion = request.Descripcion?.Trim() ?? string.Empty;
        actor.DatosContacto = request.DatosContacto?.Trim() ?? string.Empty;
        actor.Latitud = request.Latitud;
        actor.Longitud = request.Longitud;
        actor.Activo = request.Activo;

        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> Handle(EliminarActorAdminCommand request, CancellationToken cancellationToken)
    {
        var actor = await _db.ActoresMapa.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (actor == null) return Result<bool>.NotFound("Actor no encontrado.");

        _db.ActoresMapa.Remove(actor);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
