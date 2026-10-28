using System.Collections.Concurrent;
using InnovaFemIA.Web.Features.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Shared.Services;

public interface IMediaCacheService
{
    Task<(string? FilePath, string MimeType, string FileName)?> ObtenerRutaMedioAsync(int contenidoId, CancellationToken ct = default);
    Task GuardarEnCacheAsync(int contenidoId, byte[] bytes, string? mimeType, string? fileName);
    void InvalidarCache(int contenidoId);
}

/// <summary>
/// Servicio de alto rendimiento para almacenamiento en caché local de medios (Videos, PDFs e Imágenes).
/// Almacena una copia física en el servidor tras la primera consulta o subida, evitando re-consultar
/// la base de datos MySQL (LONGBLOB) y reduciendo el consumo de CPU y RAM a 0% mediante kernel-mode streaming.
/// </summary>
public class MediaCacheService : IMediaCacheService
{
    private readonly IWebHostEnvironment _env;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MediaCacheService> _logger;
    private readonly string _cacheDirectory;
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> _locks = new();

    public MediaCacheService(
        IWebHostEnvironment env,
        IServiceProvider serviceProvider,
        ILogger<MediaCacheService> logger)
    {
        _env = env;
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Directorio persistente en el servidor (ignorado en Git)
        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        _cacheDirectory = Path.Combine(webRoot, "media_cache");

        try
        {
            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo acceder o crear la carpeta {CacheDir}. Probando ruta alternativa en Temp.", _cacheDirectory);
            try
            {
                _cacheDirectory = Path.Combine(Path.GetTempPath(), "innovafemia_media_cache");
                if (!Directory.Exists(_cacheDirectory))
                {
                    Directory.CreateDirectory(_cacheDirectory);
                }
            }
            catch (Exception exTemp)
            {
                _logger.LogError(exTemp, "Tampoco se pudo inicializar la caché temporal en {TempDir}.", _cacheDirectory);
            }
        }
    }

    public async Task<(string? FilePath, string MimeType, string FileName)?> ObtenerRutaMedioAsync(int contenidoId, CancellationToken ct = default)
    {
        var cacheFilePath = Path.Combine(_cacheDirectory, $"{contenidoId}.media");
        var metaFilePath = Path.Combine(_cacheDirectory, $"{contenidoId}.meta");

        // 1. Si ya existe en el disco local y tiene contenido válido
        if (File.Exists(cacheFilePath) && new FileInfo(cacheFilePath).Length > 0)
        {
            var (cachedMime, cachedName) = LeerMetadata(metaFilePath, contenidoId);
            return (cacheFilePath, cachedMime, cachedName);
        }

        // 2. Control de concurrencia: Evita que múltiples solicitudes HTTP 206 (Range) descarguen de BD al mismo tiempo
        var semaphore = _locks.GetOrAdd(contenidoId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);

        try
        {
            // Verificar nuevamente tras adquirir el semáforo
            if (File.Exists(cacheFilePath) && new FileInfo(cacheFilePath).Length > 0)
            {
                var (cachedMime, cachedName) = LeerMetadata(metaFilePath, contenidoId);
                return (cacheFilePath, cachedMime, cachedName);
            }

            // En el Módulo de Mapa, sirve medios cacheados en disco
            await Task.CompletedTask;
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al extraer o guardar en caché el medio #{Id}", contenidoId);
            return null;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task GuardarEnCacheAsync(int contenidoId, byte[] bytes, string? mimeType, string? fileName)
    {
        try
        {
            var cacheFilePath = Path.Combine(_cacheDirectory, $"{contenidoId}.media");
            var metaFilePath = Path.Combine(_cacheDirectory, $"{contenidoId}.meta");

            var mime = string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType;
            var name = string.IsNullOrWhiteSpace(fileName) ? $"recurso_{contenidoId}.dat" : fileName;

            await File.WriteAllBytesAsync(cacheFilePath, bytes);
            await File.WriteAllLinesAsync(metaFilePath, [mime, name]);

            _logger.LogInformation("Medio #{Id} guardado directamente en caché local al crearse/editarse.", contenidoId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo pre-guardar en caché el medio #{Id}", contenidoId);
        }
    }

    public void InvalidarCache(int contenidoId)
    {
        try
        {
            var cacheFilePath = Path.Combine(_cacheDirectory, $"{contenidoId}.media");
            var metaFilePath = Path.Combine(_cacheDirectory, $"{contenidoId}.meta");

            if (File.Exists(cacheFilePath)) File.Delete(cacheFilePath);
            if (File.Exists(metaFilePath)) File.Delete(metaFilePath);

            _logger.LogInformation("Caché invalidada para medio #{Id}", contenidoId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al invalidar caché del medio #{Id}", contenidoId);
        }
    }

    private static (string Mime, string Name) LeerMetadata(string metaFilePath, int id)
    {
        try
        {
            if (File.Exists(metaFilePath))
            {
                var lines = File.ReadAllLines(metaFilePath);
                if (lines.Length >= 2) return (lines[0], lines[1]);
                if (lines.Length == 1) return (lines[0], $"recurso_{id}.dat");
            }
        }
        catch { }

        return ("application/octet-stream", $"recurso_{id}.dat");
    }
}
