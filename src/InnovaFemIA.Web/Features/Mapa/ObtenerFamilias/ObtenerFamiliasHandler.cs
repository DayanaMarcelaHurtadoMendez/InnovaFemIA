using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Mapa.ObtenerFamilias;

/// <summary>
/// Handler para ObtenerFamiliasQuery.
/// Obtiene las familias del directorio con sus cultivos y estado de stock.
/// Convierte la foto (byte[]) a Base64 para el frontend.
/// </summary>
public class ObtenerFamiliasHandler : IRequestHandler<ObtenerFamiliasQuery, Result<IEnumerable<FamiliaDto>>>
{
    private readonly AppDbContext _db;

    public ObtenerFamiliasHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<IEnumerable<FamiliaDto>>> Handle(
        ObtenerFamiliasQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Familias
            .Include(f => f.FamiliaCultivos)
                .ThenInclude(fc => fc.Cultivo)
            .Include(f => f.Stocks)
            .AsNoTracking()
            .Where(f => !request.SoloActivas || f.Activo);

        // Filtro por nombre
        if (!string.IsNullOrWhiteSpace(request.Busqueda))
        {
            var busqueda = request.Busqueda.ToLower();
            query = query.Where(f =>
                f.Nombre.ToLower().Contains(busqueda) ||
                f.FamiliaCultivos.Any(fc => fc.Cultivo.Nombre.ToLower().Contains(busqueda)));
        }

        // Filtro por cultivo específico
        if (request.CultivoId.HasValue)
        {
            query = query.Where(f =>
                f.FamiliaCultivos.Any(fc => fc.CultivoId == request.CultivoId.Value));
        }

        var familias = await query
            .OrderBy(f => f.Nombre)
            .ToListAsync(cancellationToken);

        var dtos = familias.Select(f => new FamiliaDto
        {
            Id = f.Id,
            Nombre = f.Nombre,
            Vereda = f.Vereda,
            Municipio = f.Municipio,
            NombreContacto = f.NombreContacto,
            TelefonoContacto = f.TelefonoContacto,
            Latitud = f.Latitud,
            Longitud = f.Longitud,
            FotoBase64 = f.FotoBytes is not null && f.FotoMimeType is not null
                ? $"data:{f.FotoMimeType};base64,{Convert.ToBase64String(f.FotoBytes)}"
                : null,
            Cultivos = f.FamiliaCultivos.Select(fc => new CultivStockDto
            {
                CultivoId = fc.CultivoId,
                NombreCultivo = fc.Cultivo.Nombre,
                Icono = fc.Cultivo.Icono,
                Disponible = !f.Stocks.Any(s => s.CultivoId == fc.CultivoId && !s.Disponible)
            })
        });

        return Result<IEnumerable<FamiliaDto>>.Success(dtos);
    }
}
