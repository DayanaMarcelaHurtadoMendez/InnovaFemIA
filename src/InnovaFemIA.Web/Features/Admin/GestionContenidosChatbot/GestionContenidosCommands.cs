using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Entities;
using InnovaFemIA.Web.Features.Shared.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Admin.GestionContenidosChatbot;

// DTOs
public record ContenidoChatbotAdminDto(
    int Id,
    int CultivoId,
    string NombreCultivo,
    TemaContenido Tema,
    TipoRecurso TipoRecurso,
    string Titulo,
    string? Url,
    string? ArchivoNombre,
    string? ArchivoMimeType,
    long TamanoBytes,
    bool TieneArchivoEnBd,
    int Orden,
    string? NombreFuente,
    string? EnlaceFuente,
    bool Activo,
    DateTime FechaCreacion
);

public record ArchivoContenidoDto(
    byte[] Bytes,
    string MimeType,
    string NombreArchivo
);

// Queries & Commands
public record ObtenerContenidosChatbotQuery(int? CultivoId = null, TemaContenido? Tema = null) : IRequest<Result<List<ContenidoChatbotAdminDto>>>;

public record ObtenerArchivoContenidoQuery(int Id) : IRequest<Result<ArchivoContenidoDto>>;

public record CrearContenidoChatbotCommand(
    int CultivoId,
    TemaContenido Tema,
    string Titulo,
    string? Url,
    int Orden,
    byte[]? ArchivoBytes,
    string? ArchivoMimeType,
    string? ArchivoNombre,
    string? NombreFuente,
    string? EnlaceFuente
) : IRequest<Result<int>>;

public record EditarContenidoChatbotCommand(
    int Id,
    int CultivoId,
    TemaContenido Tema,
    string Titulo,
    string? Url,
    int Orden,
    bool Activo,
    byte[]? ArchivoBytes = null,
    string? ArchivoMimeType = null,
    string? ArchivoNombre = null,
    string? NombreFuente = null,
    string? EnlaceFuente = null
) : IRequest<Result<bool>>;

public record EliminarContenidoChatbotCommand(int Id) : IRequest<Result<bool>>;

// Handlers
public class GestionContenidosChatbotHandler :
    IRequestHandler<ObtenerContenidosChatbotQuery, Result<List<ContenidoChatbotAdminDto>>>,
    IRequestHandler<ObtenerArchivoContenidoQuery, Result<ArchivoContenidoDto>>,
    IRequestHandler<CrearContenidoChatbotCommand, Result<int>>,
    IRequestHandler<EditarContenidoChatbotCommand, Result<bool>>,
    IRequestHandler<EliminarContenidoChatbotCommand, Result<bool>>
{
    private readonly AppDbContext _db;

    public GestionContenidosChatbotHandler(AppDbContext db)
    {
        _db = db;
    }

    public static TipoRecurso DeducirTipoRecurso(string? mimeType, string? archivoNombre, string? url)
    {
        var ext = !string.IsNullOrEmpty(archivoNombre) ? Path.GetExtension(archivoNombre).ToLowerInvariant() : string.Empty;
        var mime = mimeType?.ToLowerInvariant() ?? string.Empty;
        var urlLower = url?.ToLowerInvariant() ?? string.Empty;

        // 1. Detección por Video
        if (mime.StartsWith("video/") || ext == ".mp4" || ext == ".webm" || ext == ".mkv" || ext == ".avi" || ext == ".mov")
            return TipoRecurso.Video;

        // 2. Detección por Infografía / Imagen
        if (mime.StartsWith("image/") || ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".webp" || ext == ".gif")
            return TipoRecurso.Infografia;

        // 3. Detección por Documento PDF
        if (mime.Contains("pdf") || ext == ".pdf" || ext == ".doc" || ext == ".docx")
            return TipoRecurso.Documento;

        // 4. Detección por URL Externa
        if (!string.IsNullOrEmpty(urlLower))
        {
            if (urlLower.Contains("youtube.com") || urlLower.Contains("youtu.be") || urlLower.Contains("vimeo.com") || urlLower.EndsWith(".mp4") || urlLower.EndsWith(".webm"))
                return TipoRecurso.Video;

            if (urlLower.EndsWith(".png") || urlLower.EndsWith(".jpg") || urlLower.EndsWith(".jpeg") || urlLower.EndsWith(".webp"))
                return TipoRecurso.Infografia;

            if (urlLower.EndsWith(".pdf") || urlLower.Contains("drive.google.com"))
                return TipoRecurso.Documento;
        }

        return TipoRecurso.Video; // Fallback por defecto
    }

    public async Task<Result<List<ContenidoChatbotAdminDto>>> Handle(ObtenerContenidosChatbotQuery request, CancellationToken cancellationToken)
    {
        var query = _db.ContenidosChatbot.Include(c => c.Cultivo).AsQueryable();

        if (request.CultivoId.HasValue)
        {
            query = query.Where(c => c.CultivoId == request.CultivoId.Value);
        }

        if (request.Tema.HasValue)
        {
            query = query.Where(c => c.Tema == request.Tema.Value);
        }

        var contenidos = await query
            .OrderBy(c => c.Cultivo.Nombre)
            .ThenBy(c => c.Tema)
            .ThenBy(c => c.Orden)
            .Select(c => new ContenidoChatbotAdminDto(
                c.Id,
                c.CultivoId,
                c.Cultivo != null ? c.Cultivo.Nombre : "General",
                c.Tema,
                c.TipoRecurso,
                c.Titulo,
                c.Url,
                c.ArchivoNombre,
                c.ArchivoMimeType,
                c.ArchivoBytes != null ? c.ArchivoBytes.Length : 0,
                c.ArchivoBytes != null && c.ArchivoBytes.Length > 0,
                c.Orden,
                c.NombreFuente,
                c.EnlaceFuente,
                c.Activo,
                c.FechaCreacion
            ))
            .ToListAsync(cancellationToken);

        return Result<List<ContenidoChatbotAdminDto>>.Success(contenidos);
    }

    public async Task<Result<ArchivoContenidoDto>> Handle(ObtenerArchivoContenidoQuery request, CancellationToken cancellationToken)
    {
        var contenido = await _db.ContenidosChatbot
            .Where(c => c.Id == request.Id)
            .Select(c => new { c.ArchivoBytes, c.ArchivoMimeType, c.ArchivoNombre })
            .FirstOrDefaultAsync(cancellationToken);

        if (contenido == null || contenido.ArchivoBytes == null || contenido.ArchivoBytes.Length == 0)
        {
            return Result<ArchivoContenidoDto>.NotFound("El archivo solicitado no existe.");
        }

        var mime = string.IsNullOrWhiteSpace(contenido.ArchivoMimeType) ? "application/octet-stream" : contenido.ArchivoMimeType;
        var nombre = string.IsNullOrWhiteSpace(contenido.ArchivoNombre) ? $"recurso_{request.Id}.dat" : contenido.ArchivoNombre;

        return Result<ArchivoContenidoDto>.Success(new ArchivoContenidoDto(contenido.ArchivoBytes, mime, nombre));
    }

    public async Task<Result<int>> Handle(CrearContenidoChatbotCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Titulo))
            return Result<int>.BadRequest("El título del contenido es obligatorio.");

        bool tieneUrl = !string.IsNullOrWhiteSpace(request.Url);
        bool tieneArchivo = request.ArchivoBytes != null && request.ArchivoBytes.Length > 0;

        if (!tieneUrl && !tieneArchivo)
        {
            return Result<int>.BadRequest("Debe proporcionar una URL del recurso o subir un archivo (Video, PDF o Infografía).");
        }

        var tipoCalculado = DeducirTipoRecurso(request.ArchivoMimeType, request.ArchivoNombre, request.Url);

        var contenido = new ContenidoChatbot
        {
            CultivoId = request.CultivoId,
            Tema = request.Tema,
            TipoRecurso = tipoCalculado,
            Titulo = request.Titulo.Trim(),
            Url = request.Url?.Trim(),
            ArchivoBytes = request.ArchivoBytes,
            ArchivoMimeType = request.ArchivoMimeType,
            ArchivoNombre = request.ArchivoNombre,
            Orden = request.Orden <= 0 ? 1 : request.Orden,
            NombreFuente = request.NombreFuente?.Trim(),
            EnlaceFuente = request.EnlaceFuente?.Trim(),
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _db.ContenidosChatbot.Add(contenido);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(contenido.Id);
    }

    public async Task<Result<bool>> Handle(EditarContenidoChatbotCommand request, CancellationToken cancellationToken)
    {
        var contenido = await _db.ContenidosChatbot.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (contenido == null) return Result<bool>.NotFound("Contenido no encontrado.");

        if (string.IsNullOrWhiteSpace(request.Titulo))
            return Result<bool>.BadRequest("El título del contenido es obligatorio.");

        contenido.CultivoId = request.CultivoId;
        contenido.Tema = request.Tema;
        contenido.Titulo = request.Titulo.Trim();
        contenido.Url = request.Url?.Trim();
        contenido.Orden = request.Orden;
        contenido.NombreFuente = request.NombreFuente?.Trim();
        contenido.EnlaceFuente = request.EnlaceFuente?.Trim();
        contenido.Activo = request.Activo;

        if (request.ArchivoBytes != null && request.ArchivoBytes.Length > 0)
        {
            contenido.ArchivoBytes = request.ArchivoBytes;
            contenido.ArchivoMimeType = request.ArchivoMimeType;
            contenido.ArchivoNombre = request.ArchivoNombre;

            // Recalcula TipoRecurso según el nuevo archivo
            contenido.TipoRecurso = DeducirTipoRecurso(request.ArchivoMimeType, request.ArchivoNombre, request.Url);

            // Invalida la caché física en disco si existía
            var tempDir = Path.Combine(Path.GetTempPath(), "InnovaFemIA_Media");
            var cachePath = Path.Combine(tempDir, $"{contenido.Id}.media");
            if (File.Exists(cachePath))
            {
                try { File.Delete(cachePath); } catch { }
            }
        }
        else
        {
            // Mantiene el archivo actual o recalcula TipoRecurso si cambió la URL
            contenido.TipoRecurso = DeducirTipoRecurso(contenido.ArchivoMimeType, contenido.ArchivoNombre, contenido.Url);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> Handle(EliminarContenidoChatbotCommand request, CancellationToken cancellationToken)
    {
        var contenido = await _db.ContenidosChatbot.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (contenido == null) return Result<bool>.NotFound("Contenido no encontrado.");

        _db.ContenidosChatbot.Remove(contenido);
        await _db.SaveChangesAsync(cancellationToken);

        // Invalida la caché física en disco
        var tempDir = Path.Combine(Path.GetTempPath(), "InnovaFemIA_Media");
        var cachePath = Path.Combine(tempDir, $"{request.Id}.media");
        if (File.Exists(cachePath))
        {
            try { File.Delete(cachePath); } catch { }
        }

        return Result<bool>.Success(true);
    }
}
