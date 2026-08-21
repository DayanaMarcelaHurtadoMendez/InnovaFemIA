using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Entities;
using InnovaFemIA.Web.Features.Shared.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Admin.GestionCultivos;

public record CultivoAdminDto(int Id, string Nombre, string Descripcion, string Icono, bool Activo);

public record ObtenerCultivosAdminQuery : IRequest<Result<List<CultivoAdminDto>>>;

public record CrearCultivoCommand(string Nombre, string? Descripcion, string? Icono) : IRequest<Result<int>>;

public record EditarCultivoCommand(int Id, string Nombre, string? Descripcion, string? Icono, bool Activo) : IRequest<Result<bool>>;

public record EliminarCultivoCommand(int Id) : IRequest<Result<bool>>;

public class GestionCultivosHandler :
    IRequestHandler<ObtenerCultivosAdminQuery, Result<List<CultivoAdminDto>>>,
    IRequestHandler<CrearCultivoCommand, Result<int>>,
    IRequestHandler<EditarCultivoCommand, Result<bool>>,
    IRequestHandler<EliminarCultivoCommand, Result<bool>>
{
    private readonly AppDbContext _db;

    public GestionCultivosHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<CultivoAdminDto>>> Handle(ObtenerCultivosAdminQuery request, CancellationToken cancellationToken)
    {
        var cultivos = await _db.Cultivos
            .OrderBy(c => c.Nombre)
            .Select(c => new CultivoAdminDto(c.Id, c.Nombre, c.Descripcion, c.Icono, c.Activo))
            .ToListAsync(cancellationToken);

        return Result<List<CultivoAdminDto>>.Success(cultivos);
    }

    public async Task<Result<int>> Handle(CrearCultivoCommand request, CancellationToken cancellationToken)
    {
        var nombre = request.Nombre?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            return Result<int>.BadRequest("El nombre del cultivo es obligatorio.");

        if (await _db.Cultivos.AnyAsync(c => c.Nombre.ToLower() == nombre.ToLower(), cancellationToken))
            return Result<int>.BadRequest("Ya existe un cultivo con este nombre.");

        var cultivo = new Cultivo
        {
            Nombre = nombre,
            Descripcion = request.Descripcion?.Trim() ?? string.Empty,
            Icono = string.IsNullOrWhiteSpace(request.Icono) ? "🌱" : request.Icono.Trim(),
            Activo = true
        };

        _db.Cultivos.Add(cultivo);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(cultivo.Id);
    }

    public async Task<Result<bool>> Handle(EditarCultivoCommand request, CancellationToken cancellationToken)
    {
        var cultivo = await _db.Cultivos.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (cultivo == null) return Result<bool>.NotFound("Cultivo no encontrado.");

        cultivo.Nombre = request.Nombre.Trim();
        cultivo.Descripcion = request.Descripcion?.Trim() ?? string.Empty;
        cultivo.Icono = string.IsNullOrWhiteSpace(request.Icono) ? "🌱" : request.Icono.Trim();
        cultivo.Activo = request.Activo;

        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> Handle(EliminarCultivoCommand request, CancellationToken cancellationToken)
    {
        var cultivo = await _db.Cultivos.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (cultivo == null) return Result<bool>.NotFound("Cultivo no encontrado.");

        _db.Cultivos.Remove(cultivo);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
