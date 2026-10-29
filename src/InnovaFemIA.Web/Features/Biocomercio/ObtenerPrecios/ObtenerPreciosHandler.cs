using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Biocomercio.ObtenerPrecios;

public class ObtenerPreciosHandler : IRequestHandler<ObtenerPreciosQuery, Result<PreciosSipsaViewModel>>
{
    private readonly AppDbContext _db;

    public ObtenerPreciosHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PreciosSipsaViewModel>> Handle(ObtenerPreciosQuery request, CancellationToken cancellationToken)
    {
        var boletinInfo = await _db.BoletinesSipsaInfo
            .Where(b => b.EsExitoso)
            .OrderByDescending(b => b.FechaPublicacion)
            .FirstOrDefaultAsync(cancellationToken);

        BoletinSipsaInfoDto? boletinDto = null;
        if (boletinInfo != null)
        {
            boletinDto = new BoletinSipsaInfoDto
            {
                Periodo = boletinInfo.Periodo,
                BoletinTecnicoUrl = boletinInfo.BoletinTecnicoUrl,
                BoletinTecnicoTitulo = boletinInfo.BoletinTecnicoTitulo,
                AnexoExcelUrl = boletinInfo.AnexoExcelUrl,
                AnexoExcelTitulo = boletinInfo.AnexoExcelTitulo,
                FechaPublicacion = boletinInfo.FechaPublicacion,
                FechaSincronizacion = boletinInfo.FechaSincronizacion,
                EsReciente = (DateTime.UtcNow - boletinInfo.FechaSincronizacion).TotalDays <= 14
            };
        }

        var cultivos = await _db.Cultivos
            .Where(c => c.Activo)
            .ToListAsync(cancellationToken);

        var fechaLimiteReciente = DateTime.UtcNow.AddDays(-60);
        var queryBasePrecios = _db.PreciosMayoristasSipsa
            .Where(p => p.FechaActualizacion >= fechaLimiteReciente);

        var cotizacionesMayoristas = await queryBasePrecios.ToListAsync(cancellationToken);

        var preciosCultivosDtos = new List<PrecioMercadoDto>();

        foreach (var c in cultivos)
        {
            var nombreBuscar = c.Nombre.Trim().ToLower();

            var cotizacionesCultivo = cotizacionesMayoristas
                .Where(p =>
                    (p.CultivoId == c.Id ||
                    p.Producto.Contains(c.Nombre, StringComparison.OrdinalIgnoreCase) ||
                    (nombreBuscar.Equals("Café", StringComparison.OrdinalIgnoreCase) && p.Producto.Contains("Café", StringComparison.OrdinalIgnoreCase)) ||
                    (nombreBuscar.Equals("Plátano", StringComparison.OrdinalIgnoreCase) && p.Producto.Contains("Plátano", StringComparison.OrdinalIgnoreCase)) ||
                    (nombreBuscar.Equals("Yuca", StringComparison.OrdinalIgnoreCase) && p.Producto.Contains("Yuca", StringComparison.OrdinalIgnoreCase)) ||
                    (nombreBuscar.Equals("Aguacate", StringComparison.OrdinalIgnoreCase) && p.Producto.Contains("Aguacate", StringComparison.OrdinalIgnoreCase)) ||
                    (nombreBuscar.Equals("Caña", StringComparison.OrdinalIgnoreCase) && (p.Producto.Contains("Panela", StringComparison.OrdinalIgnoreCase) || p.Producto.Contains("Caña", StringComparison.OrdinalIgnoreCase))) ||
                    (nombreBuscar.Equals("Maíz", StringComparison.OrdinalIgnoreCase) && p.Producto.Contains("Maíz", StringComparison.OrdinalIgnoreCase)))
                    && ObtenerPrioridadCercaniaCaldono(p.MercadoMayorista).HasValue
                )
                .OrderBy(p => ObtenerPrioridadCercaniaCaldono(p.MercadoMayorista)!.Value)
                .ThenByDescending(p => p.PrecioMedio)
                .ToList();

            if (cotizacionesCultivo.Any())
            {
                var cotizacionCercana = cotizacionesCultivo.First();
                var tendencia = cotizacionCercana.Tendencia?.Trim() ?? "=";

                string estadoVariacion = "Igual";
                decimal pct = 0;
                if (tendencia == "+++" || tendencia == "++" || tendencia == "+")
                {
                    estadoVariacion = "Sube";
                    pct = tendencia == "+++" ? 20m : (tendencia == "++" ? 15m : 5m);
                }
                else if (tendencia == "---" || tendencia == "--" || tendencia == "-")
                {
                    estadoVariacion = "Baja";
                    pct = tendencia == "---" ? 20m : (tendencia == "--" ? 15m : 5m);
                }

                preciosCultivosDtos.Add(new PrecioMercadoDto
                {
                    Id = cotizacionCercana.Id,
                    CultivoId = c.Id,
                    NombreCultivo = c.Nombre,
                    IconoCultivo = c.Icono,
                    Precio = cotizacionCercana.PrecioMedio,
                    PrecioAnterior = cotizacionCercana.PrecioMinimo > 0 ? cotizacionCercana.PrecioMinimo : cotizacionCercana.PrecioMedio,
                    PrecioMinimo = cotizacionCercana.PrecioMinimo,
                    PrecioMaximo = cotizacionCercana.PrecioMaximo,
                    MercadoReferencia = cotizacionCercana.MercadoMayorista,
                    Tendencia = tendencia,
                    EstadoVariacion = estadoVariacion,
                    PorcentajeVariacion = pct,
                    FechaActualizacion = cotizacionCercana.FechaActualizacion,
                    Fuente = $"DANE - SIPSA ({cotizacionCercana.MercadoMayorista})",
                    EsActualizado = true
                });
            }
        }

        var querySipsa = queryBasePrecios
            .Where(p =>
                (p.MercadoMayorista.Contains("Popayán") || p.MercadoMayorista.Contains("Popayan") ||
                 p.MercadoMayorista.Contains("Cali") || p.MercadoMayorista.Contains("Cavasa") ||
                 p.MercadoMayorista.Contains("Santa Elena") || p.MercadoMayorista.Contains("Alameda") ||
                 p.MercadoMayorista.Contains("Palmira")) &&
                !p.MercadoMayorista.Contains("Buenaventura") &&
                !p.MercadoMayorista.Contains("Nariño") &&
                !p.MercadoMayorista.Contains("Narino") &&
                !p.MercadoMayorista.Contains("Ipiales") &&
                !p.MercadoMayorista.Contains("Pasto"))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Grupo))
        {
            querySipsa = querySipsa.Where(p => p.GrupoAlimento == request.Grupo);
        }

        if (!string.IsNullOrWhiteSpace(request.Busqueda))
        {
            var b = request.Busqueda.Trim().ToLower();
            querySipsa = querySipsa.Where(p =>
                p.Producto.ToLower().Contains(b) ||
                p.MercadoMayorista.ToLower().Contains(b) ||
                p.GrupoAlimento.ToLower().Contains(b));
        }

        if (!string.IsNullOrWhiteSpace(request.Mercado))
        {
            querySipsa = querySipsa.Where(p => p.MercadoMayorista == request.Mercado);
        }

        var cotizacionesDb = await querySipsa
            .OrderBy(p => p.GrupoAlimento)
            .ThenBy(p => p.Producto)
            .ThenBy(p => p.MercadoMayorista)
            .ToListAsync(cancellationToken);

        var cotizacionesDtos = cotizacionesDb.Select(p => new PrecioMayoristaSipsaDto
        {
            Id = p.Id,
            GrupoAlimento = p.GrupoAlimento,
            Producto = p.Producto,
            MercadoMayorista = p.MercadoMayorista,
            PrecioMinimo = p.PrecioMinimo,
            PrecioMaximo = p.PrecioMaximo,
            PrecioMedio = p.PrecioMedio,
            Tendencia = p.Tendencia,
            PeriodoBoletin = p.PeriodoBoletin,
            FechaActualizacion = p.FechaActualizacion,
            CultivoId = p.CultivoId
        }).ToList();

        var gruposDisponibles = await queryBasePrecios
            .Where(p =>
                (p.MercadoMayorista.Contains("Popayán") || p.MercadoMayorista.Contains("Popayan") ||
                 p.MercadoMayorista.Contains("Cali") || p.MercadoMayorista.Contains("Cavasa") ||
                 p.MercadoMayorista.Contains("Santa Elena") || p.MercadoMayorista.Contains("Alameda") ||
                 p.MercadoMayorista.Contains("Palmira")) &&
                !p.MercadoMayorista.Contains("Buenaventura") &&
                !p.MercadoMayorista.Contains("Nariño") &&
                !p.MercadoMayorista.Contains("Narino") &&
                !p.MercadoMayorista.Contains("Ipiales") &&
                !p.MercadoMayorista.Contains("Pasto"))
            .Select(p => p.GrupoAlimento)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync(cancellationToken);

        var mercadosDisponibles = await queryBasePrecios
            .Where(p =>
                (p.MercadoMayorista.Contains("Popayán") || p.MercadoMayorista.Contains("Popayan") ||
                 p.MercadoMayorista.Contains("Cali") || p.MercadoMayorista.Contains("Cavasa") ||
                 p.MercadoMayorista.Contains("Santa Elena") || p.MercadoMayorista.Contains("Alameda") ||
                 p.MercadoMayorista.Contains("Palmira")) &&
                !p.MercadoMayorista.Contains("Buenaventura") &&
                !p.MercadoMayorista.Contains("Nariño") &&
                !p.MercadoMayorista.Contains("Narino") &&
                !p.MercadoMayorista.Contains("Ipiales") &&
                !p.MercadoMayorista.Contains("Pasto"))
            .Select(p => p.MercadoMayorista)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync(cancellationToken);

        var viewModel = new PreciosSipsaViewModel
        {
            BoletinActual = boletinDto,
            PreciosCultivos = preciosCultivosDtos,
            CotizacionesSipsa = cotizacionesDtos,
            GruposDisponibles = gruposDisponibles,
            MercadosDisponibles = mercadosDisponibles,
            TotalRegistros = cotizacionesDtos.Count,
            UltimaActualizacion = boletinDto?.FechaSincronizacion ?? DateTime.UtcNow
        };

        return Result<PreciosSipsaViewModel>.Success(viewModel);
    }

    private static int? ObtenerPrioridadCercaniaCaldono(string mercado)
    {
        if (string.IsNullOrWhiteSpace(mercado)) return null;
        var m = mercado.ToLowerInvariant();

        if (m.Contains("popayán") || m.Contains("popayan")) return 1;

        if (m.Contains("cali") || m.Contains("cavasa") || m.Contains("santa elena") || m.Contains("palmira"))
        {
            if (!m.Contains("buenaventura") && !m.Contains("nariño") && !m.Contains("narino") && !m.Contains("ipiales"))
            {
                return 2;
            }
        }

        return null;
    }
}
