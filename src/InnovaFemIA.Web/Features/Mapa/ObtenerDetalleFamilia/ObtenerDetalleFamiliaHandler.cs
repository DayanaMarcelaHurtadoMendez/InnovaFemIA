using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Mapa.ObtenerDetalleFamilia;

/// <summary>
/// Handler para consultar la ficha completa y stock detallado de una familia campesina.
/// </summary>
public class ObtenerDetalleFamiliaHandler : IRequestHandler<ObtenerDetalleFamiliaQuery, Result<DetalleFamiliaDto>>
{
    private readonly AppDbContext _db;

    public ObtenerDetalleFamiliaHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<DetalleFamiliaDto>> Handle(
        ObtenerDetalleFamiliaQuery request,
        CancellationToken cancellationToken)
    {
        var familia = await _db.Familias
            .Include(f => f.FamiliaCultivos)
                .ThenInclude(fc => fc.Cultivo)
            .Include(f => f.Stocks)
                .ThenInclude(s => s.Cultivo)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == request.FamiliaId && f.Activo, cancellationToken);

        if (familia is null)
        {
            return Result<DetalleFamiliaDto>.NotFound("Familia no encontrada");
        }

        var dto = new DetalleFamiliaDto
        {
            Id = familia.Id,
            Nombre = familia.Nombre,
            Vereda = familia.Vereda,
            Municipio = familia.Municipio,
            NombreContacto = familia.NombreContacto,
            TelefonoContacto = familia.TelefonoContacto,
            Latitud = familia.Latitud,
            Longitud = familia.Longitud,
            FotoBase64 = familia.FotoBytes is not null && familia.FotoMimeType is not null
                ? $"data:{familia.FotoMimeType};base64,{Convert.ToBase64String(familia.FotoBytes)}"
                : null,
            FechaCreacion = familia.FechaCreacion,
            StockProductos = familia.Stocks.Select(s => new StockDetalleDto
            {
                CultivoId = s.CultivoId,
                NombreCultivo = s.Cultivo.Nombre,
                Icono = s.Cultivo.Icono,
                Disponible = s.Disponible,
                UltimaActualizacion = s.UltimaActualizacion,
                Notas = s.Notas
            }).ToList()
        };

        return Result<DetalleFamiliaDto>.Success(dto);
    }
}
