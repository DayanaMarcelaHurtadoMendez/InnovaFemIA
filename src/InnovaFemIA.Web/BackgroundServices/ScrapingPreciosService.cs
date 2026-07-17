using InnovaFemIA.Web.Features.Shared.Entities;
using InnovaFemIA.Web.Features.Shared.Persistence;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.BackgroundServices;

/// <summary>
/// Servicio en segundo plano que realiza web scraping periódico del DANE
/// para obtener precios actualizados de productos agrícolas.
///
/// Estrategia de robustez:
/// - Si el scraping falla, se registra el error en RegistrosSistema sin interrumpir el sistema.
/// - Los últimos precios conocidos permanecen en BD con el flag EsActualizado = false.
/// - Las vistas muestran el aviso "La información puede no estar actualizada".
///
/// Frecuencia: configurable en appsettings.json (IntervalHoras).
/// Por defecto: cada 6 horas.
/// </summary>
public class ScrapingPreciosService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ScrapingPreciosService> _logger;
    private readonly IConfiguration _configuration;

    public ScrapingPreciosService(
        IServiceProvider services,
        ILogger<ScrapingPreciosService> logger,
        IConfiguration configuration)
    {
        _services = services;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHoras = _configuration.GetValue<int>("Scraping:IntervalHoras", 6);
        var delayInicial = _configuration.GetValue<int>("Scraping:DelayInicialMinutos", 2);

        _logger.LogInformation(
            "ScrapingPreciosService iniciado. Delay inicial: {Delay} min. Intervalo: {Interval} h.",
            delayInicial, intervalHoras);

        // Delay inicial para no saturar el startup de la app
        await Task.Delay(TimeSpan.FromMinutes(delayInicial), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await EjecutarScrapingAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(intervalHoras), stoppingToken);
        }
    }

    private async Task EjecutarScrapingAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            _logger.LogInformation("Iniciando scraping de precios DANE...");

            var precios = await ObtenerPreciosDaneAsync();

            if (!precios.Any())
            {
                await RegistrarErrorAsync(db, "El scraping del DANE no retornó precios. Verificar estructura HTML.", cancellationToken);
                await MarcarPreciosDesactualizadosAsync(db, cancellationToken);
                return;
            }

            var cultivos = await db.Cultivos.Where(c => c.Activo).ToListAsync(cancellationToken);

            foreach (var (nombreCultivo, nuevoPrecio) in precios)
            {
                var cultivo = cultivos.FirstOrDefault(c =>
                    c.Nombre.Contains(nombreCultivo, StringComparison.OrdinalIgnoreCase));

                if (cultivo is null) continue;

                // Obtener el precio anterior más reciente
                var precioAnterior = await db.PreciosMercado
                    .Where(p => p.CultivoId == cultivo.Id)
                    .OrderByDescending(p => p.FechaActualizacion)
                    .Select(p => (decimal?)p.Precio)
                    .FirstOrDefaultAsync(cancellationToken);

                db.PreciosMercado.Add(new PrecioMercado
                {
                    CultivoId = cultivo.Id,
                    Precio = nuevoPrecio,
                    PrecioAnterior = precioAnterior,
                    FechaActualizacion = DateTime.UtcNow,
                    Fuente = "DANE - Sipsa",
                    EsActualizado = true
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Scraping completado. {Count} precios actualizados.", precios.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el scraping de precios DANE.");
            await RegistrarErrorAsync(db, $"Error de scraping: {ex.Message}", cancellationToken, ex.StackTrace);
            await MarcarPreciosDesactualizadosAsync(db, cancellationToken);
        }
    }

    /// <summary>
    /// Realiza el web scraping del DANE (Sipsa) para obtener precios agrícolas.
    /// Retorna un diccionario de nombre de cultivo → precio por kg.
    /// </summary>
    private async Task<Dictionary<string, decimal>> ObtenerPreciosDaneAsync()
    {
        var precios = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var urlDane = _configuration.GetValue<string>(
                "Scraping:UrlDane",
                "https://www.dane.gov.co/index.php/estadisticas-por-tema/agropecuario/sistema-de-informacion-de-precios-sipsa/mayoristas-boletin-semanal-1");

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 InnovaFemIA-Bot/1.0 (Proyecto ASCAMPESINA ICESI)");

            var html = await httpClient.GetStringAsync(urlDane);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            // TODO: Ajustar el selector XPath según la estructura actual del DANE/Sipsa.
            // Esta es una implementación base que deberá actualizarse cuando se acceda
            // al sitio real del DANE y se inspeccione la estructura HTML.
            var filas = htmlDoc.DocumentNode
                .SelectNodes("//table[contains(@class,'table')]//tr[position()>1]");

            if (filas is not null)
            {
                foreach (var fila in filas)
                {
                    var celdas = fila.SelectNodes("td");
                    if (celdas?.Count >= 2)
                    {
                        var nombreProducto = celdas[0].InnerText.Trim();
                        var precioTexto = celdas[1].InnerText
                            .Trim()
                            .Replace("$", "")
                            .Replace(".", "")
                            .Replace(",", ".")
                            .Trim();

                        if (decimal.TryParse(precioTexto, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var precio))
                        {
                            precios[nombreProducto] = precio;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo completar el scraping del DANE.");
        }

        return precios;
    }

    private async Task MarcarPreciosDesactualizadosAsync(AppDbContext db, CancellationToken ct)
    {
        // Marcar todos los precios que actualmente figuran como actualizados a 'false'
        await db.PreciosMercado
            .Where(p => p.EsActualizado)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.EsActualizado, false), ct);
    }

    private async Task RegistrarErrorAsync(
        AppDbContext db, string descripcion, CancellationToken ct, string? stackTrace = null)
    {
        db.RegistrosSistema.Add(new RegistroSistema
        {
            TipoEvento = "ScrapingError",
            Descripcion = descripcion,
            Modulo = "Scraping",
            StackTrace = stackTrace,
            FechaHoraUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }
}
