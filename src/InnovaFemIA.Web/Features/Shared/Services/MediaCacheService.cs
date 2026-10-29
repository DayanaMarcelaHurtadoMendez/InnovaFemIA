using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Shared.Services;

public interface IMediaCacheService
{
    Task<(string? FilePath, string MimeType, string FileName)?> ObtenerRutaMedioAsync(int contenidoId, CancellationToken ct = default);
    Task GuardarEnCacheAsync(int contenidoId, byte[] bytes, string? mimeType, string? fileName);
    void InvalidarCache(int contenidoId);
}

/// <summary>
/// Servicio de almacenamiento en caché de medios locales en disco.
/// </summary>
public class MediaCacheService : IMediaCacheService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MediaCacheService> _logger;
    private readonly string _cacheDirectory;

    public MediaCacheService(
        IWebHostEnvironment env,
        ILogger<MediaCacheService> logger)
    {
        _env = env;
        _logger = logger;

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
            _logger.LogWarning(ex, "No se pudo acceder o crear la carpeta {CacheDir}.", _cacheDirectory);
        }
    }

    public Task<(string? FilePath, string MimeType, string FileName)?> ObtenerRutaMedioAsync(int contenidoId, CancellationToken ct = default)
    {
        var cacheFilePath = Path.Combine(_cacheDirectory, $"{contenidoId}.media");
        var metaFilePath = Path.Combine(_cacheDirectory, $"{contenidoId}.meta");

        if (File.Exists(cacheFilePath) && new FileInfo(cacheFilePath).Length > 0)
        {
            var (cachedMime, cachedName) = LeerMetadata(metaFilePath, contenidoId);
            return Task.FromResult<(string? FilePath, string MimeType, string FileName)?>((cacheFilePath, cachedMime, cachedName));
        }

        return Task.FromResult<(string? FilePath, string MimeType, string FileName)?>(null);
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
            await File.WriteAllLinesAsync(metaFilePath, new[] { mime, name });
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
