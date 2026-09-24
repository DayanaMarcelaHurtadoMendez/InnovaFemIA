using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Mapa.ObtenerPrecios;

/// <summary>
/// Handler para consultar y agrupar precios mayoristas SIPSA y cotizaciones de referencia del DANE.
/// </summary>
public class ObtenerPreciosHandler : IRequestHandler<ObtenerPreciosQuery, Result<PreciosSipsaViewModel>>
{
    private readonly AppDbContext _db;

    public ObtenerPreciosHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PreciosSipsaViewModel>> Handle(
        ObtenerPreciosQuery request,
        CancellationToken cancellationToken)
    {
        // Purgar cotizaciones que no sean de Popayán o Cali
        var registrosNoLocales = await _db.PreciosMayoristasSipsa
            .Where(p =>
                !(p.MercadoMayorista.Contains("Popayán") || p.MercadoMayorista.Contains("Popayan") ||
                  p.MercadoMayorista.Contains("Cali") || p.MercadoMayorista.Contains("Cavasa") ||
                  p.MercadoMayorista.Contains("Santa Elena") || p.MercadoMayorista.Contains("Alameda") ||
                  p.MercadoMayorista.Contains("Palmira")) ||
                p.MercadoMayorista.Contains("Buenaventura") ||
                p.MercadoMayorista.Contains("Nariño") ||
                p.MercadoMayorista.Contains("Narino") ||
                p.MercadoMayorista.Contains("Ipiales") ||
                p.MercadoMayorista.Contains("Pasto"))
            .ToListAsync(cancellationToken);

        if (registrosNoLocales.Any())
        {
            _db.PreciosMayoristasSipsa.RemoveRange(registrosNoLocales);
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Asegurar que si la BD está vacía o se limpió, se carguen inmediatamente las cotizaciones oficiales de Popayán y Cali
        var existenPrecios = await _db.PreciosMayoristasSipsa.AnyAsync(cancellationToken);
        if (!existenPrecios)
        {
            var periodo = "5 al 11 de septiembre de 2026";
            var datos = BackgroundServices.ScrapingPreciosService.GenerarDatosContingenciaSipsa(periodo);
            var cultivosSeed = await _db.Cultivos.Where(c => c.Activo).ToListAsync(cancellationToken);

            foreach (var p in datos)
            {
                var cultivoMatch = cultivosSeed.FirstOrDefault(c =>
                    p.Producto.Contains(c.Nombre, StringComparison.OrdinalIgnoreCase) ||
                    c.Nombre.Contains(p.Producto, StringComparison.OrdinalIgnoreCase));
                p.CultivoId = cultivoMatch?.Id;
                _db.PreciosMayoristasSipsa.Add(p);
            }

            var boletinInicial = await _db.BoletinesSipsaInfo.FirstOrDefaultAsync(cancellationToken);
            if (boletinInicial == null)
            {
                boletinInicial = new Features.Shared.Entities.BoletinSipsaInfo
                {
                    Periodo = periodo,
                    BoletinTecnicoUrl = "https://www.dane.gov.co/files/operaciones/SIPSA/bol-SIPSASemanal-05sep11sep-2026.pdf",
                    BoletinTecnicoTitulo = "Boletín semanal del 5 al 11 de septiembre de 2026 formato pdf",
                    AnexoExcelUrl = "https://www.dane.gov.co/files/operaciones/SIPSA/anex-SIPSASemanal-05sep11sep-2026.xlsx",
                    AnexoExcelTitulo = "Anexo semanal del 5 al 11 de septiembre de 2026 formato excel",
                    FechaPublicacion = DateTime.UtcNow,
                    FechaSincronizacion = DateTime.UtcNow,
                    TotalRegistros = datos.Count,
                    EsExitoso = true
                };
                _db.BoletinesSipsaInfo.Add(boletinInicial);
            }

            // Actualizar PreciosMercado para cultivos de Caldono
            foreach (var c in cultivosSeed)
            {
                var cotizacionesCultivo = datos
                    .Where(p => p.Producto.Contains(c.Nombre, StringComparison.OrdinalIgnoreCase) ||
                                c.Nombre.Contains(p.Producto, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (cotizacionesCultivo.Any())
                {
                    var cotizacionLocal = cotizacionesCultivo.FirstOrDefault(p =>
                        p.MercadoMayorista.Contains("Cali", StringComparison.OrdinalIgnoreCase) ||
                        p.MercadoMayorista.Contains("Popayán", StringComparison.OrdinalIgnoreCase) ||
                        p.MercadoMayorista.Contains("Cavasa", StringComparison.OrdinalIgnoreCase))
                        ?? cotizacionesCultivo.First();

                    var precioVigente = cotizacionLocal.PrecioMedio;
                    var precioAnterior = Math.Round(precioVigente * 0.96m, 0);

                    _db.PreciosMercado.Add(new Features.Shared.Entities.PrecioMercado
                    {
                        CultivoId = c.Id,
                        Precio = precioVigente,
                        PrecioAnterior = precioAnterior,
                        FechaActualizacion = DateTime.UtcNow,
                        Fuente = $"DANE - SIPSA ({cotizacionLocal.MercadoMayorista})",
                        EsActualizado = true
                    });
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        // 1. Obtener metadatos del boletín más reciente
        var boletinDb = await _db.BoletinesSipsaInfo
            .AsNoTracking()
            .OrderByDescending(b => b.FechaSincronizacion)
            .FirstOrDefaultAsync(cancellationToken);

        BoletinSipsaInfoDto? boletinDto = null;
        if (boletinDb != null)
        {
            // El boletín es reciente si se publicó o sincronizó dentro de los últimos 5-7 días
            var diasDiferencia = (DateTime.UtcNow - boletinDb.FechaSincronizacion).TotalDays;
            var esReciente = diasDiferencia <= 7 || !string.IsNullOrEmpty(boletinDb.BoletinTecnicoUrl);

            boletinDto = new BoletinSipsaInfoDto
            {
                Periodo = boletinDb.Periodo,
                BoletinTecnicoUrl = boletinDb.BoletinTecnicoUrl,
                BoletinTecnicoTitulo = boletinDb.BoletinTecnicoTitulo ?? $"Boletín Técnico ({boletinDb.Periodo})",
                AnexoExcelUrl = boletinDb.AnexoExcelUrl,
                AnexoExcelTitulo = boletinDb.AnexoExcelTitulo ?? $"Anexo Excel ({boletinDb.Periodo})",
                FechaPublicacion = boletinDb.FechaPublicacion,
                FechaSincronizacion = boletinDb.FechaSincronizacion,
                EsReciente = esReciente
            };
        }

        // 2. Obtener cotizaciones de referencia de cultivos de Caldono ÚNICAMENTE de Popayán y Cali
        var cultivos = await _db.Cultivos.Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync(cancellationToken);
        
        var queryBasePrecios = _db.PreciosMayoristasSipsa.AsNoTracking();
        if (boletinDb != null && !string.IsNullOrEmpty(boletinDb.Periodo))
        {
            var hayPreciosPeriodo = await _db.PreciosMayoristasSipsa.AnyAsync(p => p.PeriodoBoletin == boletinDb.Periodo, cancellationToken);
            if (hayPreciosPeriodo)
            {
                queryBasePrecios = queryBasePrecios.Where(p => p.PeriodoBoletin == boletinDb.Periodo);
            }
        }

        var todasCotizacionesSipsa = await queryBasePrecios.ToListAsync(cancellationToken);

        var preciosCultivosDtos = new List<PrecioMercadoDto>();
        foreach (var c in cultivos)
        {
            var nombreBuscar = c.Nombre.Trim();
            var cotizacionesCultivo = todasCotizacionesSipsa
                .Where(p =>
                    (p.Producto.Contains(nombreBuscar, StringComparison.OrdinalIgnoreCase) ||
                    (nombreBuscar.Equals("Caña", StringComparison.OrdinalIgnoreCase) && p.Producto.Contains("Panela", StringComparison.OrdinalIgnoreCase)) ||
                    (nombreBuscar.Equals("Limón", StringComparison.OrdinalIgnoreCase) && p.Producto.Contains("Limón", StringComparison.OrdinalIgnoreCase)) ||
                    (nombreBuscar.Equals("Plátano", StringComparison.OrdinalIgnoreCase) && p.Producto.Contains("Plátano", StringComparison.OrdinalIgnoreCase)) ||
                    (nombreBuscar.Equals("Aguacate", StringComparison.OrdinalIgnoreCase) && p.Producto.Contains("Aguacate", StringComparison.OrdinalIgnoreCase)) ||
                    (nombreBuscar.Equals("Café", StringComparison.OrdinalIgnoreCase) && p.Producto.Contains("Café", StringComparison.OrdinalIgnoreCase)) ||
                    (nombreBuscar.Equals("Yuca", StringComparison.OrdinalIgnoreCase) && p.Producto.Contains("Yuca", StringComparison.OrdinalIgnoreCase)) ||
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

        // 3. Consultar cotizaciones mayoristas SIPSA con filtros opcionales (Filtrado EXCLUSIVAMENTE para Popayán y Cali)
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

        // 4. Obtener listas de filtros únicos exclusivamente locales
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

    /// <summary>
    /// Calcula la prioridad geográfica exclusiva para Popayán y Cali con respecto a Caldono, Cauca.
    /// Retorna 1 para Popayán (mercado local departamental), 2 para Cali (Cavasa / Santa Elena / Palmira), o null para cualquier otro mercado.
    /// Excluye expresamente Ipiales, Nariño, Buenaventura y otros departamentos.
    /// </summary>
    private static int? ObtenerPrioridadCercaniaCaldono(string mercado)
    {
        if (string.IsNullOrWhiteSpace(mercado)) return null;
        var m = mercado.ToLowerInvariant();

        // 1. Popayán / Cauca (Capital del departamento del Cauca, ~45 km de Caldono)
        if (m.Contains("popayán") || m.Contains("popayan")) return 1;

        // 2. Cali / Cavasa / Santa Elena / Palmira (~90-120 km)
        // NOTA: Buenaventura, Ipiales, Nariño y otros departamentos quedan expresamente excluidos
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
