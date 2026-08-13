using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Entities;
using InnovaFemIA.Web.Features.Shared.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Admin.GestionFamilias;

// DTOs
public record FamiliaAdminDto(
    int Id,
    string Nombre,
    string Vereda,
    string Municipio,
    string NombreContacto,
    string TelefonoContacto,
    double Latitud,
    double Longitud,
    bool Activo,
    List<int> CultivosIds,
    List<string> CultivosNombres
);

// Queries & Commands
public record ObtenerFamiliasAdminQuery(string? Busqueda = null) : IRequest<Result<List<FamiliaAdminDto>>>;

public record CrearFamiliaAdminCommand(
    string Nombre,
    string Vereda,
    string Municipio,
    string NombreContacto,
    string TelefonoContacto,
    double Latitud,
    double Longitud,
    List<int> CultivosIds
) : IRequest<Result<int>>;

public record EditarFamiliaAdminCommand(
    int Id,
    string Nombre,
    string Vereda,
    string Municipio,
    string NombreContacto,
    string TelefonoContacto,
    double Latitud,
    double Longitud,
    bool Activo,
    List<int> CultivosIds
) : IRequest<Result<bool>>;

public record EliminarFamiliaAdminCommand(int Id) : IRequest<Result<bool>>;

// Handlers
public class GestionFamiliasHandler :
    IRequestHandler<ObtenerFamiliasAdminQuery, Result<List<FamiliaAdminDto>>>,
    IRequestHandler<CrearFamiliaAdminCommand, Result<int>>,
    IRequestHandler<EditarFamiliaAdminCommand, Result<bool>>,
    IRequestHandler<EliminarFamiliaAdminCommand, Result<bool>>
{
    private readonly AppDbContext _db;

    public GestionFamiliasHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<FamiliaAdminDto>>> Handle(ObtenerFamiliasAdminQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Familias.Include(f => f.FamiliaCultivos).ThenInclude(fc => fc.Cultivo).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Busqueda))
        {
            var term = request.Busqueda.Trim().ToLower();
            query = query.Where(f => f.Nombre.ToLower().Contains(term) || f.Vereda.ToLower().Contains(term));
        }

        var familias = await query
            .OrderBy(f => f.Nombre)
            .Select(f => new FamiliaAdminDto(
                f.Id,
                f.Nombre,
                f.Vereda,
                f.Municipio,
                f.NombreContacto,
                f.TelefonoContacto,
                f.Latitud,
                f.Longitud,
                f.Activo,
                f.FamiliaCultivos.Select(fc => fc.CultivoId).ToList(),
                f.FamiliaCultivos.Select(fc => fc.Cultivo.Nombre).ToList()
            ))
            .ToListAsync(cancellationToken);

        return Result<List<FamiliaAdminDto>>.Success(familias);
    }

    public async Task<Result<int>> Handle(CrearFamiliaAdminCommand request, CancellationToken cancellationToken)
    {
        // Validaciones según Sección 9.1 y 9.8 de la documentación ejecutiva
        if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Length < 3 || request.Nombre.Length > 100)
            return Result<int>.BadRequest("El nombre de la familia debe tener entre 3 y 100 caracteres.");

        if (string.IsNullOrWhiteSpace(request.Vereda) || request.Vereda.Length < 3 || request.Vereda.Length > 80)
            return Result<int>.BadRequest("La vereda debe tener entre 3 y 80 caracteres.");

        if (request.Latitud < -4.23 || request.Latitud > 13.39 || request.Longitud < -79.00 || request.Longitud > -66.85)
            return Result<int>.BadRequest("Las coordenadas geográficas deben estar dentro del territorio de Colombia.");

        var familia = new Familia
        {
            Nombre = request.Nombre.Trim(),
            Vereda = request.Vereda.Trim(),
            Municipio = string.IsNullOrWhiteSpace(request.Municipio) ? "Caldono" : request.Municipio.Trim(),
            NombreContacto = request.NombreContacto.Trim(),
            TelefonoContacto = request.TelefonoContacto.Trim(),
            Latitud = request.Latitud,
            Longitud = request.Longitud,
            Activo = true
        };

        _db.Familias.Add(familia);
        await _db.SaveChangesAsync(cancellationToken);

        // Agregar Cultivos y Stock inicial
        if (request.CultivosIds != null && request.CultivosIds.Any())
        {
            foreach (var cultivoId in request.CultivosIds)
            {
                _db.FamiliaCultivos.Add(new FamiliaCultivo { FamiliaId = familia.Id, CultivoId = cultivoId });
                _db.Stocks.Add(new Stock { FamiliaId = familia.Id, CultivoId = cultivoId, Disponible = true, Notas = "Cosecha disponible en finca" });
            }
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result<int>.Success(familia.Id);
    }

    public async Task<Result<bool>> Handle(EditarFamiliaAdminCommand request, CancellationToken cancellationToken)
    {
        var familia = await _db.Familias.Include(f => f.FamiliaCultivos).FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);
        if (familia == null) return Result<bool>.NotFound("Familia no encontrada.");

        if (request.Latitud < -4.23 || request.Latitud > 13.39 || request.Longitud < -79.00 || request.Longitud > -66.85)
            return Result<bool>.BadRequest("Las coordenadas geográficas deben estar dentro del territorio de Colombia.");

        familia.Nombre = request.Nombre.Trim();
        familia.Vereda = request.Vereda.Trim();
        familia.Municipio = string.IsNullOrWhiteSpace(request.Municipio) ? "Caldono" : request.Municipio.Trim();
        familia.NombreContacto = request.NombreContacto.Trim();
        familia.TelefonoContacto = request.TelefonoContacto.Trim();
        familia.Latitud = request.Latitud;
        familia.Longitud = request.Longitud;
        familia.Activo = request.Activo;

        // Actualizar cultivos
        _db.FamiliaCultivos.RemoveRange(familia.FamiliaCultivos);

        if (request.CultivosIds != null && request.CultivosIds.Any())
        {
            foreach (var cultivoId in request.CultivosIds)
            {
                _db.FamiliaCultivos.Add(new FamiliaCultivo { FamiliaId = familia.Id, CultivoId = cultivoId });
                if (!await _db.Stocks.AnyAsync(s => s.FamiliaId == familia.Id && s.CultivoId == cultivoId, cancellationToken))
                {
                    _db.Stocks.Add(new Stock { FamiliaId = familia.Id, CultivoId = cultivoId, Disponible = true, Notas = "Cosecha disponible en finca" });
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> Handle(EliminarFamiliaAdminCommand request, CancellationToken cancellationToken)
    {
        var familia = await _db.Familias.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);
        if (familia == null) return Result<bool>.NotFound("Familia no encontrada.");

        _db.Familias.Remove(familia);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
