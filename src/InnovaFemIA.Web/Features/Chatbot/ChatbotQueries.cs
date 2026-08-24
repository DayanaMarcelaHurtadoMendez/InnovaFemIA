using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Entities;
using InnovaFemIA.Web.Features.Shared.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Chatbot;

// ─── DTOs ───────────────────────────────────────────────────────────────────

public record CultivoChatbotDto(
    int Id,
    string Nombre,
    string Icono,
    string? Descripcion,
    int TotalContenidos
);

public record TemaDisponibleDto(
    string Id,
    int Numero,
    string Nombre,
    string Icono,
    int TotalRecursos
);

public record ContenidoChatbotItemDto(
    int Id,
    int CultivoId,
    string NombreCultivo,
    TemaContenido Tema,
    TipoRecurso TipoRecurso,
    string Titulo,
    string? Url,
    bool TieneArchivo,
    string? ArchivoNombre,
    string? ArchivoMimeType,
    long TamanoBytes,
    int Orden
);

public record ArchivoChatbotDto(
    byte[] Bytes,
    string MimeType,
    string NombreArchivo
);

public record LeccionResumenDto(
    int Id,
    int NumeroLeccion,
    string Titulo,
    string Descripcion,
    bool TieneImagen,
    int TotalPreguntas
);

public record OpcionPreguntaDto(
    int Id,
    string TextoOpcion,
    bool EsCorrecta,
    string? Retroalimentacion
);

public record PreguntaLeccionDto(
    int Id,
    int Orden,
    string TextoPregunta,
    string? ContenidoExplicativo,
    bool TieneImagenExplicativa,
    List<OpcionPreguntaDto> Opciones
);

public record LeccionDetalleDto(
    int Id,
    int NumeroLeccion,
    string Titulo,
    string Descripcion,
    bool TieneImagen,
    List<PreguntaLeccionDto> Preguntas
);

// ─── Queries ────────────────────────────────────────────────────────────────

public record ObtenerCultivosChatbotQuery() : IRequest<Result<List<CultivoChatbotDto>>>;

public record ObtenerTemasPorCultivoQuery(int CultivoId) : IRequest<Result<List<TemaDisponibleDto>>>;

public record ObtenerContenidosPorTemaQuery(int CultivoId, TemaContenido Tema) : IRequest<Result<List<ContenidoChatbotItemDto>>>;

public record ObtenerDetalleRecursoQuery(int Id) : IRequest<Result<ContenidoChatbotItemDto>>;

public record ObtenerArchivoRecursoQuery(int Id) : IRequest<Result<ArchivoChatbotDto>>;

public record ObtenerLeccionesEducativasQuery() : IRequest<Result<List<LeccionResumenDto>>>;

public record ObtenerDetalleLeccionQuery(int LeccionId) : IRequest<Result<LeccionDetalleDto>>;

public record ObtenerImagenLeccionQuery(int LeccionId) : IRequest<Result<ArchivoChatbotDto>>;

public record ObtenerImagenPreguntaQuery(int PreguntaId) : IRequest<Result<ArchivoChatbotDto>>;

// ─── Handlers ───────────────────────────────────────────────────────────────

public class ChatbotQueriesHandler :
    IRequestHandler<ObtenerCultivosChatbotQuery, Result<List<CultivoChatbotDto>>>,
    IRequestHandler<ObtenerTemasPorCultivoQuery, Result<List<TemaDisponibleDto>>>,
    IRequestHandler<ObtenerContenidosPorTemaQuery, Result<List<ContenidoChatbotItemDto>>>,
    IRequestHandler<ObtenerDetalleRecursoQuery, Result<ContenidoChatbotItemDto>>,
    IRequestHandler<ObtenerArchivoRecursoQuery, Result<ArchivoChatbotDto>>,
    IRequestHandler<ObtenerLeccionesEducativasQuery, Result<List<LeccionResumenDto>>>,
    IRequestHandler<ObtenerDetalleLeccionQuery, Result<LeccionDetalleDto>>,
    IRequestHandler<ObtenerImagenLeccionQuery, Result<ArchivoChatbotDto>>,
    IRequestHandler<ObtenerImagenPreguntaQuery, Result<ArchivoChatbotDto>>
{
    private readonly AppDbContext _db;

    public ChatbotQueriesHandler(AppDbContext db)
    {
        _db = db;
    }

    public static (string Nombre, string Icono) ObtenerNombreEIconoTema(TemaContenido tema) => tema switch
    {
        TemaContenido.CuandoSembrar => ("Cuándo Sembrar", "🌱"),
        TemaContenido.CuandoCosechar => ("Cuándo Cosechar", "🌾"),
        TemaContenido.PlagasEnfermedades => ("Plagas y Enfermedades", "🐛"),
        TemaContenido.Abonado => ("Abonado y Nutrición", "🧪"),
        TemaContenido.PodaMantenimiento => ("Poda y Mantenimiento", "✂️"),
        TemaContenido.RiegoDrenaje => ("Riego y Drenaje", "💧"),
        TemaContenido.PostcosechaManejo => ("Cosecha y Postcosecha", "📦"),
        TemaContenido.ManejoAgroecologico => ("Manejo Agroecológico", "🌿"),
        TemaContenido.ComercializacionCalidad => ("Calidad y Comercialización", "🏷️"),
        _ => (tema.ToString(), "🌱")
    };

    public async Task<Result<List<CultivoChatbotDto>>> Handle(ObtenerCultivosChatbotQuery request, CancellationToken cancellationToken)
    {
        // Solo retornar cultivos activos que SI tengan contenidos registrados en BD
        var cultivos = await _db.Cultivos
            .Where(c => c.Activo && c.ContenidosChatbot.Any(cc => cc.Activo))
            .OrderBy(c => c.Nombre)
            .Select(c => new CultivoChatbotDto(
                c.Id,
                c.Nombre,
                string.IsNullOrEmpty(c.Icono) ? "🌱" : c.Icono,
                c.Descripcion,
                c.ContenidosChatbot.Count(cc => cc.Activo)
            ))
            .ToListAsync(cancellationToken);

        return Result<List<CultivoChatbotDto>>.Success(cultivos);
    }

    public async Task<Result<List<TemaDisponibleDto>>> Handle(ObtenerTemasPorCultivoQuery request, CancellationToken cancellationToken)
    {
        // Solo retornar temas que SI tengan contenido cargado para este cultivo específico
        var temasRegistrados = await _db.ContenidosChatbot
            .Where(c => c.CultivoId == request.CultivoId && c.Activo)
            .GroupBy(c => c.Tema)
            .Select(g => new { Tema = g.Key, Total = g.Count() })
            .ToListAsync(cancellationToken);

        var resultado = temasRegistrados.Select(t => {
            var (nombre, icono) = ObtenerNombreEIconoTema(t.Tema);
            return new TemaDisponibleDto(
                t.Tema.ToString(),
                (int)t.Tema,
                nombre,
                icono,
                t.Total
            );
        }).OrderBy(t => t.Numero).ToList();

        return Result<List<TemaDisponibleDto>>.Success(resultado);
    }

    public async Task<Result<List<ContenidoChatbotItemDto>>> Handle(ObtenerContenidosPorTemaQuery request, CancellationToken cancellationToken)
    {
        var contenidos = await _db.ContenidosChatbot
            .Include(c => c.Cultivo)
            .Where(c => c.CultivoId == request.CultivoId && c.Tema == request.Tema && c.Activo)
            .OrderBy(c => c.Orden)
            .Select(c => new ContenidoChatbotItemDto(
                c.Id,
                c.CultivoId,
                c.Cultivo != null ? c.Cultivo.Nombre : "Cultivo",
                c.Tema,
                c.TipoRecurso,
                c.Titulo,
                c.Url,
                c.ArchivoBytes != null && c.ArchivoBytes.Length > 0,
                c.ArchivoNombre,
                c.ArchivoMimeType,
                c.ArchivoBytes != null ? c.ArchivoBytes.Length : 0,
                c.Orden
            ))
            .ToListAsync(cancellationToken);

        return Result<List<ContenidoChatbotItemDto>>.Success(contenidos);
    }

    public async Task<Result<ContenidoChatbotItemDto>> Handle(ObtenerDetalleRecursoQuery request, CancellationToken cancellationToken)
    {
        var c = await _db.ContenidosChatbot
            .Include(x => x.Cultivo)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.Activo, cancellationToken);

        if (c == null)
            return Result<ContenidoChatbotItemDto>.NotFound("Recurso no encontrado.");

        var dto = new ContenidoChatbotItemDto(
            c.Id,
            c.CultivoId,
            c.Cultivo != null ? c.Cultivo.Nombre : "Cultivo",
            c.Tema,
            c.TipoRecurso,
            c.Titulo,
            c.Url,
            c.ArchivoBytes != null && c.ArchivoBytes.Length > 0,
            c.ArchivoNombre,
            c.ArchivoMimeType,
            c.ArchivoBytes != null ? c.ArchivoBytes.Length : 0,
            c.Orden
        );

        return Result<ContenidoChatbotItemDto>.Success(dto);
    }

    public async Task<Result<ArchivoChatbotDto>> Handle(ObtenerArchivoRecursoQuery request, CancellationToken cancellationToken)
    {
        var contenido = await _db.ContenidosChatbot
            .Where(c => c.Id == request.Id)
            .Select(c => new { c.ArchivoBytes, c.ArchivoMimeType, c.ArchivoNombre })
            .FirstOrDefaultAsync(cancellationToken);

        if (contenido == null || contenido.ArchivoBytes == null || contenido.ArchivoBytes.Length == 0)
        {
            return Result<ArchivoChatbotDto>.NotFound("El archivo solicitado no existe.");
        }

        var mime = string.IsNullOrWhiteSpace(contenido.ArchivoMimeType) ? "application/octet-stream" : contenido.ArchivoMimeType;
        var nombre = string.IsNullOrWhiteSpace(contenido.ArchivoNombre) ? $"recurso_{request.Id}.dat" : contenido.ArchivoNombre;

        return Result<ArchivoChatbotDto>.Success(new ArchivoChatbotDto(contenido.ArchivoBytes, mime, nombre));
    }

    public async Task<Result<List<LeccionResumenDto>>> Handle(ObtenerLeccionesEducativasQuery request, CancellationToken cancellationToken)
    {
        var lecciones = await _db.LeccionesChatbot
            .Where(l => l.Activo)
            .OrderBy(l => l.NumeroLeccion)
            .Select(l => new LeccionResumenDto(
                l.Id,
                l.NumeroLeccion,
                l.Titulo,
                l.Descripcion,
                l.ImagenBytes != null && l.ImagenBytes.Length > 0,
                l.Preguntas.Count
            ))
            .ToListAsync(cancellationToken);

        return Result<List<LeccionResumenDto>>.Success(lecciones);
    }

    public async Task<Result<LeccionDetalleDto>> Handle(ObtenerDetalleLeccionQuery request, CancellationToken cancellationToken)
    {
        var leccion = await _db.LeccionesChatbot
            .Include(l => l.Preguntas.OrderBy(p => p.Orden))
                .ThenInclude(p => p.Opciones)
            .FirstOrDefaultAsync(l => l.Id == request.LeccionId && l.Activo, cancellationToken);

        if (leccion == null)
            return Result<LeccionDetalleDto>.NotFound("Lección no encontrada.");

        var preguntasDto = leccion.Preguntas.Select(p => new PreguntaLeccionDto(
            p.Id,
            p.Orden,
            p.TextoPregunta,
            p.ContenidoExplicativo,
            p.ImagenExplicativaBytes != null && p.ImagenExplicativaBytes.Length > 0,
            p.Opciones.Select(o => new OpcionPreguntaDto(
                o.Id,
                o.TextoOpcion,
                o.EsCorrecta,
                o.Retroalimentacion
            )).ToList()
        )).ToList();

        var dto = new LeccionDetalleDto(
            leccion.Id,
            leccion.NumeroLeccion,
            leccion.Titulo,
            leccion.Descripcion,
            leccion.ImagenBytes != null && leccion.ImagenBytes.Length > 0,
            preguntasDto
        );

        return Result<LeccionDetalleDto>.Success(dto);
    }

    public async Task<Result<ArchivoChatbotDto>> Handle(ObtenerImagenLeccionQuery request, CancellationToken cancellationToken)
    {
        var l = await _db.LeccionesChatbot
            .Where(x => x.Id == request.LeccionId)
            .Select(x => new { x.ImagenBytes, x.ImagenMimeType })
            .FirstOrDefaultAsync(cancellationToken);

        if (l == null || l.ImagenBytes == null || l.ImagenBytes.Length == 0)
            return Result<ArchivoChatbotDto>.NotFound("Imagen no encontrada.");

        var mime = string.IsNullOrWhiteSpace(l.ImagenMimeType) ? "image/jpeg" : l.ImagenMimeType;
        return Result<ArchivoChatbotDto>.Success(new ArchivoChatbotDto(l.ImagenBytes, mime, $"leccion_{request.LeccionId}.jpg"));
    }

    public async Task<Result<ArchivoChatbotDto>> Handle(ObtenerImagenPreguntaQuery request, CancellationToken cancellationToken)
    {
        var p = await _db.PreguntasLecciones
            .Where(x => x.Id == request.PreguntaId)
            .Select(x => new { x.ImagenExplicativaBytes, x.ImagenExplicativaMimeType })
            .FirstOrDefaultAsync(cancellationToken);

        if (p == null || p.ImagenExplicativaBytes == null || p.ImagenExplicativaBytes.Length == 0)
            return Result<ArchivoChatbotDto>.NotFound("Imagen explicativa no encontrada.");

        var mime = string.IsNullOrWhiteSpace(p.ImagenExplicativaMimeType) ? "image/jpeg" : p.ImagenExplicativaMimeType;
        return Result<ArchivoChatbotDto>.Success(new ArchivoChatbotDto(p.ImagenExplicativaBytes, mime, $"pregunta_{request.PreguntaId}.jpg"));
    }
}
