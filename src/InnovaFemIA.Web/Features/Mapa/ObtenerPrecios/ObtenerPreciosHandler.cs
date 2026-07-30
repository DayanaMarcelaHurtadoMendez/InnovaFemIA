using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Mapa.ObtenerPrecios;

/// <summary>
/// Handler para consultar y calcular la variación de precios de mercado del DANE.
/// </summary>
public class ObtenerPreciosHandler : IRequestHandler<ObtenerPreciosQuery, Result<IEnumerable<PrecioMercadoDto>>>
{
    private readonly AppDbContext _db;

    public ObtenerPreciosHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<IEnumerable<PrecioMercadoDto>>> Handle(
        ObtenerPreciosQuery request,
        CancellationToken cancellationToken)
    {
        // Obtener el registro de precio más reciente por cada cultivo
        var ultimosPreciosIds = await _db.PreciosMercado
            .GroupBy(p => p.CultivoId)
            .Select(g => g.OrderByDescending(p => p.FechaActualizacion).Select(p => p.Id).FirstOrDefault())
            .ToListAsync(cancellationToken);

        var precios = await _db.PreciosMercado
            .Include(p => p.Cultivo)
            .AsNoTracking()
            .Where(p => ultimosPreciosIds.Contains(p.Id))
            .OrderBy(p => p.Cultivo.Nombre)
            .ToListAsync(cancellationToken);

        var dtos = precios.Select(p =>
        {
            var variacion = "Igual";
            decimal pct = 0;

            if (p.PrecioAnterior.HasValue && p.PrecioAnterior.Value > 0)
            {
                var dif = p.Precio - p.PrecioAnterior.Value;
                pct = Math.Round((dif / p.PrecioAnterior.Value) * 100, 1);
                if (dif > 0) variacion = "Sube";
                else if (dif < 0) variacion = "Baja";
            }

            return new PrecioMercadoDto
            {
                Id = p.Id,
                CultivoId = p.CultivoId,
                NombreCultivo = p.Cultivo.Nombre,
                IconoCultivo = p.Cultivo.Icono,
                Precio = p.Precio,
                PrecioAnterior = p.PrecioAnterior,
                EstadoVariacion = variacion,
                PorcentajeVariacion = pct,
                FechaActualizacion = p.FechaActualizacion,
                Fuente = p.Fuente,
                EsActualizado = p.EsActualizado
            };
        });

        return Result<IEnumerable<PrecioMercadoDto>>.Success(dtos);
    }
}
