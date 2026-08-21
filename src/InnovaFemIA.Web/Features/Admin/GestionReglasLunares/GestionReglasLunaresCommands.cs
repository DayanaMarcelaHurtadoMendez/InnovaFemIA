using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Entities;
using InnovaFemIA.Web.Features.Shared.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Admin.GestionReglasLunares;

// DTOs
public record ReglaLunarAdminDto(
    int Id,
    FaseLunar FaseLunar,
    string Practica,
    string Recomendacion,
    string NivelConfianza,
    int? CultivoId,
    string? NombreCultivo
);

// Queries & Commands
public record ObtenerReglasLunaresAdminQuery(FaseLunar? Fase = null) : IRequest<Result<List<ReglaLunarAdminDto>>>;

public record CrearReglaLunarCommand(
    FaseLunar FaseLunar,
    string Practica,
    string Recomendacion,
    string NivelConfianza,
    int? CultivoId
) : IRequest<Result<int>>;

public record EditarReglaLunarCommand(
    int Id,
    FaseLunar FaseLunar,
    string Practica,
    string Recomendacion,
    string NivelConfianza,
    int? CultivoId
) : IRequest<Result<bool>>;

public record EliminarReglaLunarCommand(int Id) : IRequest<Result<bool>>;

// Handlers
public class GestionReglasLunaresHandler :
    IRequestHandler<ObtenerReglasLunaresAdminQuery, Result<List<ReglaLunarAdminDto>>>,
    IRequestHandler<CrearReglaLunarCommand, Result<int>>,
    IRequestHandler<EditarReglaLunarCommand, Result<bool>>,
    IRequestHandler<EliminarReglaLunarCommand, Result<bool>>
{
    private readonly AppDbContext _db;

    public GestionReglasLunaresHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<ReglaLunarAdminDto>>> Handle(ObtenerReglasLunaresAdminQuery request, CancellationToken cancellationToken)
    {
        var query = _db.ReglasLunares.Include(r => r.Cultivo).AsQueryable();

        if (request.Fase.HasValue)
        {
            query = query.Where(r => r.FaseLunar == request.Fase.Value);
        }

        var reglas = await query
            .OrderBy(r => r.FaseLunar)
            .ThenBy(r => r.Practica)
            .Select(r => new ReglaLunarAdminDto(
                r.Id,
                r.FaseLunar,
                r.Practica,
                r.Recomendacion,
                r.NivelConfianza ?? "Alta",
                r.CultivoId,
                r.Cultivo != null ? r.Cultivo.Nombre : "Todos los cultivos"
            ))
            .ToListAsync(cancellationToken);

        return Result<List<ReglaLunarAdminDto>>.Success(reglas);
    }

    public async Task<Result<int>> Handle(CrearReglaLunarCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Practica) || string.IsNullOrWhiteSpace(request.Recomendacion))
            return Result<int>.BadRequest("La práctica y la recomendación son obligatorias.");

        var regla = new ReglasLunares
        {
            FaseLunar = request.FaseLunar,
            Practica = request.Practica.Trim(),
            Recomendacion = request.Recomendacion.Trim(),
            NivelConfianza = string.IsNullOrWhiteSpace(request.NivelConfianza) ? "Alta" : request.NivelConfianza.Trim(),
            CultivoId = request.CultivoId
        };

        _db.ReglasLunares.Add(regla);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(regla.Id);
    }

    public async Task<Result<bool>> Handle(EditarReglaLunarCommand request, CancellationToken cancellationToken)
    {
        var regla = await _db.ReglasLunares.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        if (regla == null) return Result<bool>.NotFound("Regla lunar no encontrada.");

        regla.FaseLunar = request.FaseLunar;
        regla.Practica = request.Practica.Trim();
        regla.Recomendacion = request.Recomendacion.Trim();
        regla.NivelConfianza = request.NivelConfianza.Trim();
        regla.CultivoId = request.CultivoId;

        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> Handle(EliminarReglaLunarCommand request, CancellationToken cancellationToken)
    {
        var regla = await _db.ReglasLunares.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        if (regla == null) return Result<bool>.NotFound("Regla lunar no encontrada.");

        _db.ReglasLunares.Remove(regla);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
