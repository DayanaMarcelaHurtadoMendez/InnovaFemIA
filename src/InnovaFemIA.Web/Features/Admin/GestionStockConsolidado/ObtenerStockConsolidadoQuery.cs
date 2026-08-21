using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Admin.GestionStockConsolidado;

public record StockConsolidadoFamiliaDto(
    int FamiliaId,
    string NombreFamilia,
    string Vereda,
    string Contacto,
    string Telefono,
    List<ItemStockDto> ProductosDisponibles
);

public record ItemStockDto(int CultivoId, string NombreCultivo, string Icono, bool Disponible, DateTime UltimaActualizacion, string? Notas);

public record ObtenerStockConsolidadoQuery : IRequest<Result<List<StockConsolidadoFamiliaDto>>>;

public class ObtenerStockConsolidadoHandler : IRequestHandler<ObtenerStockConsolidadoQuery, Result<List<StockConsolidadoFamiliaDto>>>
{
    private readonly AppDbContext _db;

    public ObtenerStockConsolidadoHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<StockConsolidadoFamiliaDto>>> Handle(ObtenerStockConsolidadoQuery request, CancellationToken cancellationToken)
    {
        var familiasStock = await _db.Familias
            .Include(f => f.Stocks)
            .ThenInclude(s => s.Cultivo)
            .OrderBy(f => f.Nombre)
            .Select(f => new StockConsolidadoFamiliaDto(
                f.Id,
                f.Nombre,
                f.Vereda,
                f.NombreContacto,
                f.TelefonoContacto,
                f.Stocks.Select(s => new ItemStockDto(
                    s.CultivoId,
                    s.Cultivo.Nombre,
                    s.Cultivo.Icono,
                    s.Disponible,
                    s.UltimaActualizacion,
                    s.Notas
                )).ToList()
            ))
            .ToListAsync(cancellationToken);

        return Result<List<StockConsolidadoFamiliaDto>>.Success(familiasStock);
    }
}
