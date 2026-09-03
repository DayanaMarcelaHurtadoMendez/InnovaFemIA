using InnovaFemIA.Web.Features.Shared.Persistence;
using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Biocomercio.Stock;

// 1. Obtener Stock de la Familia
public record ObtenerStockFamiliaQuery(int FamiliaId) : IRequest<Result<StockViewModel>>;

public class StockViewModel
{
    public int FamiliaId { get; set; }
    public List<StockItemDto> Items { get; set; } = new();
    public List<FamiliaConStockDto> FamiliasConMismosProductos { get; set; } = new();
}

public class StockItemDto
{
    public int CultivoId { get; set; }
    public string NombreCultivo { get; set; } = string.Empty;
    public string Icono { get; set; } = string.Empty;
    public bool Disponible { get; set; }
}

public class FamiliaConStockDto
{
    public int FamiliaId { get; set; }
    public string NombreFamilia { get; set; } = string.Empty;
    public string Vereda { get; set; } = string.Empty;
    public string TelefonoContacto { get; set; } = string.Empty;
    public List<string> ProductosCoincidentes { get; set; } = new();
}

public class ObtenerStockFamiliaQueryHandler : IRequestHandler<ObtenerStockFamiliaQuery, Result<StockViewModel>>
{
    private readonly AppDbContext _db;

    public ObtenerStockFamiliaQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<StockViewModel>> Handle(ObtenerStockFamiliaQuery request, CancellationToken cancellationToken)
    {
        // Traer TODOS los cultivos activos del sistema para que la familia pueda elegir nuevos
        var todosLosCultivos = await _db.Cultivos
            .Where(c => c.Activo)
            .Select(c => new
            {
                c.Id,
                c.Nombre,
                c.Icono
            })
            .ToListAsync(cancellationToken);

        var stockActual = await _db.Stocks
            .Where(s => s.FamiliaId == request.FamiliaId)
            .ToListAsync(cancellationToken);

        var viewModel = new StockViewModel { FamiliaId = request.FamiliaId };

        foreach (var c in todosLosCultivos)
        {
            var item = stockActual.FirstOrDefault(s => s.CultivoId == c.Id);
            viewModel.Items.Add(new StockItemDto
            {
                CultivoId = c.Id,
                NombreCultivo = c.Nombre,
                Icono = c.Icono,
                Disponible = item?.Disponible ?? false
            });
        }

        // Obtener otras familias con los mismos productos disponibles
        var cultivosDisponibles = viewModel.Items.Where(i => i.Disponible).Select(i => i.CultivoId).ToList();
        
        if (cultivosDisponibles.Any())
        {
            var otrasFamiliasDb = await _db.Stocks
                .Include(s => s.Familia)
                .Include(s => s.Cultivo)
                .Where(s => s.Familia.Activo && s.FamiliaId != request.FamiliaId && s.Disponible)
                .ToListAsync(cancellationToken);

            viewModel.FamiliasConMismosProductos = otrasFamiliasDb
                .Where(s => cultivosDisponibles.Contains(s.CultivoId))
                .GroupBy(s => s.FamiliaId)
                .Select(g => new FamiliaConStockDto
                {
                    FamiliaId = g.Key,
                    NombreFamilia = g.First().Familia.Nombre,
                    Vereda = g.First().Familia.Vereda,
                    TelefonoContacto = g.First().Familia.TelefonoContacto,
                    ProductosCoincidentes = g.Select(s => s.Cultivo.Nombre).ToList()
                })
                .ToList();
        }

        return Result<StockViewModel>.Success(viewModel);
    }
}

// 2. Actualizar Stock
public record ActualizarStockFamiliaCommand(int FamiliaId, List<int> CultivosDisponiblesIds) : IRequest<Result>;

public class ActualizarStockFamiliaCommandHandler : IRequestHandler<ActualizarStockFamiliaCommand, Result>
{
    private readonly AppDbContext _db;

    public ActualizarStockFamiliaCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(ActualizarStockFamiliaCommand request, CancellationToken cancellationToken)
    {
        var stocksExistentes = await _db.Stocks
            .Where(s => s.FamiliaId == request.FamiliaId)
            .ToListAsync(cancellationToken);

        // Los que ya existen y se actualiza su estado
        foreach (var stock in stocksExistentes)
        {
            bool ahoraDisponible = request.CultivosDisponiblesIds.Contains(stock.CultivoId);
            if (stock.Disponible != ahoraDisponible)
            {
                stock.Disponible = ahoraDisponible;
                stock.UltimaActualizacion = DateTime.UtcNow;
            }
        }

        // Los que son nuevos y vienen como disponibles
        var nuevosIds = request.CultivosDisponiblesIds.Except(stocksExistentes.Select(s => s.CultivoId)).ToList();
        
        // Obtener la relación existente para agregar a la familia los cultivos que seleccione y no tenga
        var familiaCultivosActuales = await _db.FamiliaCultivos
            .Where(fc => fc.FamiliaId == request.FamiliaId)
            .Select(fc => fc.CultivoId)
            .ToListAsync(cancellationToken);

        foreach (var nid in nuevosIds)
        {
            _db.Stocks.Add(new InnovaFemIA.Web.Features.Shared.Entities.Stock
            {
                FamiliaId = request.FamiliaId,
                CultivoId = nid,
                Disponible = true,
                UltimaActualizacion = DateTime.UtcNow
            });

            // Si el cultivo no estaba asignado a su perfil, asignarlo automáticamente
            if (!familiaCultivosActuales.Contains(nid))
            {
                _db.FamiliaCultivos.Add(new InnovaFemIA.Web.Features.Shared.Entities.FamiliaCultivo
                {
                    FamiliaId = request.FamiliaId,
                    CultivoId = nid
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
