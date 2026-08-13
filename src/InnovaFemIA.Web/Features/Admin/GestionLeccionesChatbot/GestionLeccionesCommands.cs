using InnovaFemIA.Web.Features.Shared.Common;
using InnovaFemIA.Web.Features.Shared.Entities;
using InnovaFemIA.Web.Features.Shared.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InnovaFemIA.Web.Features.Admin.GestionLeccionesChatbot;

// DTOs
public record OpcionPreguntaDto(int Id, string TextoOpcion, bool EsCorrecta, string? Retroalimentacion);

public record PreguntaLeccionDto(int Id, int LeccionId, string TextoPregunta, string? ContenidoExplicativo, int Orden, List<OpcionPreguntaDto> Opciones);

public record LeccionChatbotAdminDto(
    int Id,
    int NumeroLeccion,
    string Titulo,
    string Descripcion,
    bool Activo,
    DateTime FechaCreacion,
    int TotalPreguntas,
    List<PreguntaLeccionDto> Preguntas
);

// Queries & Commands
public record ObtenerLeccionesAdminQuery : IRequest<Result<List<LeccionChatbotAdminDto>>>;

public record CrearLeccionCommand(
    int NumeroLeccion,
    string Titulo,
    string Descripcion
) : IRequest<Result<int>>;

public record AgregarPreguntaLeccionCommand(
    int LeccionId,
    string TextoPregunta,
    string? ContenidoExplicativo,
    int Orden,
    List<OpcionPreguntaInput> Opciones
) : IRequest<Result<int>>;

public record OpcionPreguntaInput(string TextoOpcion, bool EsCorrecta, string? Retroalimentacion);

public record EliminarLeccionCommand(int Id) : IRequest<Result<bool>>;
public record EliminarPreguntaCommand(int Id) : IRequest<Result<bool>>;

// Handlers
public class GestionLeccionesHandler :
    IRequestHandler<ObtenerLeccionesAdminQuery, Result<List<LeccionChatbotAdminDto>>>,
    IRequestHandler<CrearLeccionCommand, Result<int>>,
    IRequestHandler<AgregarPreguntaLeccionCommand, Result<int>>,
    IRequestHandler<EliminarLeccionCommand, Result<bool>>,
    IRequestHandler<EliminarPreguntaCommand, Result<bool>>
{
    private readonly AppDbContext _db;

    public GestionLeccionesHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<LeccionChatbotAdminDto>>> Handle(ObtenerLeccionesAdminQuery request, CancellationToken cancellationToken)
    {
        var lecciones = await _db.LeccionesChatbot
            .Include(l => l.Preguntas)
            .ThenInclude(p => p.Opciones)
            .OrderBy(l => l.NumeroLeccion)
            .Select(l => new LeccionChatbotAdminDto(
                l.Id,
                l.NumeroLeccion,
                l.Titulo,
                l.Descripcion,
                l.Activo,
                l.FechaCreacion,
                l.Preguntas.Count,
                l.Preguntas.OrderBy(p => p.Orden).Select(p => new PreguntaLeccionDto(
                    p.Id,
                    p.LeccionId,
                    p.TextoPregunta,
                    p.ContenidoExplicativo,
                    p.Orden,
                    p.Opciones.Select(o => new OpcionPreguntaDto(o.Id, o.TextoOpcion, o.EsCorrecta, o.Retroalimentacion)).ToList()
                )).ToList()
            ))
            .ToListAsync(cancellationToken);

        return Result<List<LeccionChatbotAdminDto>>.Success(lecciones);
    }

    public async Task<Result<int>> Handle(CrearLeccionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Titulo))
            return Result<int>.BadRequest("El título de la lección es obligatorio.");

        if (await _db.LeccionesChatbot.AnyAsync(l => l.NumeroLeccion == request.NumeroLeccion, cancellationToken))
            return Result<int>.BadRequest($"Ya existe la Lección número {request.NumeroLeccion}. Elige otro número.");

        var leccion = new LeccionChatbot
        {
            NumeroLeccion = request.NumeroLeccion,
            Titulo = request.Titulo.Trim(),
            Descripcion = request.Descripcion?.Trim() ?? string.Empty,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _db.LeccionesChatbot.Add(leccion);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(leccion.Id);
    }

    public async Task<Result<int>> Handle(AgregarPreguntaLeccionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TextoPregunta))
            return Result<int>.BadRequest("El texto de la pregunta es obligatorio.");

        // Regla Sección 9.8: Cada pregunta requiere entre 3 y 4 opciones y exactamente una marcada como correcta
        if (request.Opciones == null || request.Opciones.Count < 3 || request.Opciones.Count > 4)
        {
            return Result<int>.BadRequest("Cada pregunta debe tener entre 3 y 4 opciones de respuesta.");
        }

        if (request.Opciones.Count(o => o.EsCorrecta) != 1)
        {
            return Result<int>.BadRequest("Debe haber exactamente una opción marcada como correcta.");
        }

        var pregunta = new PreguntaLeccion
        {
            LeccionId = request.LeccionId,
            TextoPregunta = request.TextoPregunta.Trim(),
            ContenidoExplicativo = request.ContenidoExplicativo?.Trim(),
            Orden = request.Orden <= 0 ? 1 : request.Orden
        };

        _db.PreguntasLecciones.Add(pregunta);
        await _db.SaveChangesAsync(cancellationToken);

        foreach (var op in request.Opciones)
        {
            _db.OpcionesPregunta.Add(new OpcionPregunta
            {
                PreguntaId = pregunta.Id,
                TextoOpcion = op.TextoOpcion.Trim(),
                EsCorrecta = op.EsCorrecta,
                Retroalimentacion = op.Retroalimentacion?.Trim()
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(pregunta.Id);
    }

    public async Task<Result<bool>> Handle(EliminarLeccionCommand request, CancellationToken cancellationToken)
    {
        // Regla Sección 9.8: Al eliminar una lección, eliminar también sus preguntas y opciones asociadas en cascada dentro de 1 sola transacción
        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var leccion = await _db.LeccionesChatbot.FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);
            if (leccion == null) return Result<bool>.NotFound("Lección no encontrada.");

            _db.LeccionesChatbot.Remove(leccion);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result<bool>.Success(true);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Result<bool>> Handle(EliminarPreguntaCommand request, CancellationToken cancellationToken)
    {
        var pregunta = await _db.PreguntasLecciones.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (pregunta == null) return Result<bool>.NotFound("Pregunta no encontrada.");

        _db.PreguntasLecciones.Remove(pregunta);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
