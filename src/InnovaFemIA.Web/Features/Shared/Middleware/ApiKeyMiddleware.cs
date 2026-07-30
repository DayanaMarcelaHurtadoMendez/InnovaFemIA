using InnovaFemIA.Web.Features.Shared.Attributes;
using InnovaFemIA.Web.Features.Shared.Persistence;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace InnovaFemIA.Web.Features.Shared.Middleware;

/// <summary>
/// Middleware que controla el acceso a los endpoints de API (/api/).
///
/// Lógica:
/// 1. Si la ruta NO comienza con "/api/" → pasa al siguiente middleware (no aplica).
/// 2. Si el endpoint NO tiene el atributo [PublicApi] → 403 Forbidden.
///    (Los endpoints internos sólo son consumidos desde el código del servidor, no desde HTTP externo.)
/// 3. Si el endpoint tiene [PublicApi]:
///    a. Busca la API Key en header "X-Api-Key" o query string "?apikey=".
///    b. Hashea la clave recibida con SHA-256 y la compara contra la tabla ApiKeys en MySQL.
///    c. Si no se encontró o no está activa → 401 Unauthorized.
///    d. Si es válida → actualiza UltimoUso + UltimaIpUso → next().
/// </summary>
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;

    private const string ApiKeyHeaderName = "X-Api-Key";
    private const string ApiKeyQueryName = "apikey";

    public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Solo aplica a rutas /api/
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Permitir peticiones internas originadas por el mismo frontend de la aplicación (same-origin / referer)
        if (IsSameOriginRequest(context))
        {
            await _next(context);
            return;
        }

        // Verificar si el endpoint tiene el atributo [PublicApi]
        var endpoint = context.Features.Get<IEndpointFeature>()?.Endpoint;
        var hasPublicApiAttribute = endpoint?.Metadata.GetMetadata<PublicApiAttribute>() is not null;

        if (!hasPublicApiAttribute)
        {
            _logger.LogWarning("Intento de acceso externo a endpoint interno: {Path} desde {IP}",
                path, context.Connection.RemoteIpAddress);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                """{"error":"Acceso denegado. Este endpoint es de uso interno exclusivo.","code":403}""");
            return;
        }

        // Obtener la API Key del header o query string
        string? rawApiKey = null;

        if (context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var headerValue))
        {
            rawApiKey = headerValue.FirstOrDefault();
        }
        else if (context.Request.Query.TryGetValue(ApiKeyQueryName, out var queryValue))
        {
            rawApiKey = queryValue.FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(rawApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                """{"error":"API Key requerida. Envíe la clave en el header 'X-Api-Key' o en el parámetro 'apikey'.","code":401}""");
            return;
        }

        // Hashear la clave recibida (SHA-256) para comparar con la almacenada
        var hashedKey = HashApiKey(rawApiKey);

        var apiKey = await db.ApiKeys
            .FirstOrDefaultAsync(k => k.Clave == hashedKey && k.Activo);

        if (apiKey is null)
        {
            _logger.LogWarning("API Key inválida o inactiva intentó acceder a: {Path} desde {IP}",
                path, context.Connection.RemoteIpAddress);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                """{"error":"API Key inválida o inactiva.","code":401}""");
            return;
        }

        // API Key válida → actualizar auditoría
        apiKey.UltimoUso = DateTime.UtcNow;
        apiKey.UltimaIpUso = context.Connection.RemoteIpAddress?.ToString();
        await db.SaveChangesAsync();

        // Inyectar el nombre de la app en los items del contexto (disponible en controllers)
        context.Items["ApiKeyApp"] = apiKey.NombreApp;

        await _next(context);
    }

    /// <summary>
    /// Hashea una API Key en texto plano usando SHA-256.
    /// Las claves se almacenan hasheadas en la BD para no exponer el valor real.
    /// </summary>
    private static string HashApiKey(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Determina si una petición HTTP se originó desde la misma interfaz web de la aplicación (same-origin).
    /// </summary>
    private static bool IsSameOriginRequest(HttpContext context)
    {
        // 1. Header Sec-Fetch-Site (enviado por navegadores modernos)
        if (context.Request.Headers.TryGetValue("Sec-Fetch-Site", out var fetchSite))
        {
            var site = fetchSite.ToString();
            if (site.Equals("same-origin", StringComparison.OrdinalIgnoreCase) ||
                site.Equals("same-site", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // 2. Comprobar Referer vs Host
        if (context.Request.Headers.TryGetValue("Referer", out var refererHeader))
        {
            var referer = refererHeader.ToString();
            var host = context.Request.Host.Value;
            if (!string.IsNullOrEmpty(referer) && !string.IsNullOrEmpty(host) && referer.Contains(host, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
