using System.Collections.Concurrent;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace InnovaFemIA.Web.Features.Shared.Middleware;

/// <summary>
/// Extensiones y middleware para servir archivos estáticos con tipos MIME personalizados
/// y resolución insensible a mayúsculas/minúsculas en sistemas operativos Linux.
/// </summary>
public static class CaseInsensitiveStaticFilesExtensions
{
    private static readonly ConcurrentDictionary<string, string?> StaticPathCache = new(StringComparer.OrdinalIgnoreCase);

    public static IApplicationBuilder UseCaseInsensitiveStaticFiles(this WebApplication app)
    {
        var contentTypeProvider = new FileExtensionContentTypeProvider();
        contentTypeProvider.Mappings[".geojson"] = "application/geo+json";
        contentTypeProvider.Mappings[".webp"] = "image/webp";
        contentTypeProvider.Mappings[".png"] = "image/png";
        contentTypeProvider.Mappings[".jpeg"] = "image/jpeg";
        contentTypeProvider.Mappings[".jpg"] = "image/jpeg";
        contentTypeProvider.Mappings[".svg"] = "image/svg+xml";

        var webRoot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");

        // Middleware para resolver archivos estáticos sin importar mayúsculas/minúsculas en Linux
        app.Use(async (context, next) =>
        {
            var reqPath = context.Request.Path.Value;
            if (!string.IsNullOrEmpty(reqPath) && reqPath.Length > 1 && reqPath.Contains('.') &&
                (reqPath.StartsWith("/images/", StringComparison.OrdinalIgnoreCase) ||
                 reqPath.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
                 reqPath.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
                 reqPath.StartsWith("/data/", StringComparison.OrdinalIgnoreCase) ||
                 reqPath.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) ||
                 reqPath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)))
            {
                if (StaticPathCache.TryGetValue(reqPath, out var cachedPath))
                {
                    if (cachedPath != null) context.Request.Path = cachedPath;
                }
                else
                {
                    var resolved = ResolveCaseInsensitivePath(webRoot, reqPath);
                    StaticPathCache.TryAdd(reqPath, resolved);
                    if (resolved != null) context.Request.Path = resolved;
                }
            }
            await next();
        });

        var staticFileOptions = new StaticFileOptions
        {
            ContentTypeProvider = contentTypeProvider
        };

        app.UseDefaultFiles();
        app.UseStaticFiles(staticFileOptions);

        // Servidor específico para /images mapeado físicamente si la subcarpeta existe
        var imagesFolder = Path.Combine(webRoot, "images");
        if (Directory.Exists(imagesFolder))
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(imagesFolder),
                RequestPath = "/images",
                ContentTypeProvider = contentTypeProvider
            });
        }

        return app;
    }

    private static string? ResolveCaseInsensitivePath(string root, string urlPath)
    {
        try
        {
            var segments = urlPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var current = root;
            var resultSegments = new List<string>();

            for (int i = 0; i < segments.Length; i++)
            {
                var seg = segments[i];
                bool isLast = (i == segments.Length - 1);

                if (!Directory.Exists(current)) return null;

                if (isLast)
                {
                    var fileMatch = Directory.GetFiles(current)
                        .FirstOrDefault(f => Path.GetFileName(f).Equals(seg, StringComparison.OrdinalIgnoreCase));
                    if (fileMatch != null)
                    {
                        resultSegments.Add(Path.GetFileName(fileMatch));
                        return "/" + string.Join("/", resultSegments);
                    }
                }

                var dirMatch = Directory.GetDirectories(current)
                    .FirstOrDefault(d => Path.GetFileName(d).Equals(seg, StringComparison.OrdinalIgnoreCase));
                if (dirMatch != null)
                {
                    current = dirMatch;
                    resultSegments.Add(Path.GetFileName(dirMatch));
                    continue;
                }

                return null;
            }

            return "/" + string.Join("/", resultSegments);
        }
        catch
        {
            return null;
        }
    }
}
