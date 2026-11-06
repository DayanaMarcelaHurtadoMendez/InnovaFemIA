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

public record EditarLeccionCommand(
    int Id,
    int NumeroLeccion,
    string Titulo,
    string? Descripcion
) : IRequest<Result<bool>>;

public record EditarPreguntaLeccionCommand(
    int Id,
    string TextoPregunta,
    string? ContenidoExplicativo,
    int Orden,
    List<OpcionPreguntaInput> Opciones
) : IRequest<Result<bool>>;

// Handlers
public class GestionLeccionesHandler :
    IRequestHandler<ObtenerLeccionesAdminQuery, Result<List<LeccionChatbotAdminDto>>>,
    IRequestHandler<CrearLeccionCommand, Result<int>>,
    IRequestHandler<EditarLeccionCommand, Result<bool>>,
    IRequestHandler<AgregarPreguntaLeccionCommand, Result<int>>,
    IRequestHandler<EditarPreguntaLeccionCommand, Result<bool>>,
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

        // Cada pregunta requiere al menos 2 opciones y exactamente una marcada como correcta
        if (request.Opciones == null || request.Opciones.Count < 2)
        {
            return Result<int>.BadRequest("Cada pregunta debe tener al menos 2 opciones de respuesta.");
        }

        if (request.Opciones.Any(o => string.IsNullOrWhiteSpace(o.TextoOpcion)))
        {
            return Result<int>.BadRequest("Todas las opciones agregadas deben tener un texto válido.");
        }

        if (request.Opciones.Count(o => o.EsCorrecta) != 1)
        {
            return Result<int>.BadRequest("Debe haber exactamente una opción marcada como respuesta correcta.");
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
        try
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                var leccion = await _db.LeccionesChatbot.FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);
                if (leccion == null) return Result<bool>.NotFound("Lección no encontrada.");

                var opciones = await _db.OpcionesPregunta
                    .Where(o => o.Pregunta.LeccionId == request.Id)
                    .ToListAsync(cancellationToken);

                if (opciones.Any())
                {
                    _db.OpcionesPregunta.RemoveRange(opciones);
                }

                var preguntas = await _db.PreguntasLecciones
                    .Where(p => p.LeccionId == request.Id)
                    .ToListAsync(cancellationToken);

                if (preguntas.Any())
                {
                    _db.PreguntasLecciones.RemoveRange(preguntas);
                }

                _db.LeccionesChatbot.Remove(leccion);
                await _db.SaveChangesAsync(cancellationToken);

                return Result<bool>.Success(true);
            });
        }
        catch (Exception ex)
        {
            return Result<bool>.BadRequest($"Error al eliminar la lección: {ex.Message}");
        }
    }

    public async Task<Result<bool>> Handle(EliminarPreguntaCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                var pregunta = await _db.PreguntasLecciones.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
                if (pregunta == null) return Result<bool>.NotFound("Pregunta no encontrada.");

                var opciones = await _db.OpcionesPregunta
                    .Where(o => o.PreguntaId == request.Id)
                    .ToListAsync(cancellationToken);

                if (opciones.Any())
                {
                    _db.OpcionesPregunta.RemoveRange(opciones);
                }

                _db.PreguntasLecciones.Remove(pregunta);
                await _db.SaveChangesAsync(cancellationToken);

                return Result<bool>.Success(true);
            });
        }
        catch (Exception ex)
        {
            return Result<bool>.BadRequest($"Error al eliminar la pregunta: {ex.Message}");
        }
    }

    public async Task<Result<bool>> Handle(EditarLeccionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Titulo))
            return Result<bool>.BadRequest("El título de la lección es obligatorio.");

        try
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                var leccion = await _db.LeccionesChatbot.FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);
                if (leccion == null) return Result<bool>.NotFound("Lección no encontrada.");

                if (leccion.NumeroLeccion != request.NumeroLeccion)
                {
                    if (await _db.LeccionesChatbot.AnyAsync(l => l.Id != request.Id && l.NumeroLeccion == request.NumeroLeccion, cancellationToken))
                    {
                        return Result<bool>.BadRequest($"Ya existe otra Lección con el número {request.NumeroLeccion}. Elige otro número.");
                    }
                }

                leccion.NumeroLeccion = request.NumeroLeccion;
                leccion.Titulo = request.Titulo.Trim();
                leccion.Descripcion = request.Descripcion?.Trim() ?? string.Empty;

                await _db.SaveChangesAsync(cancellationToken);
                return Result<bool>.Success(true);
            });
        }
        catch (Exception ex)
        {
            return Result<bool>.BadRequest($"Error al actualizar la lección: {ex.Message}");
        }
    }

    public async Task<Result<bool>> Handle(EditarPreguntaLeccionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TextoPregunta))
            return Result<bool>.BadRequest("El texto de la pregunta es obligatorio.");

        if (request.Opciones == null || request.Opciones.Count < 2)
        {
            return Result<bool>.BadRequest("Cada pregunta debe tener al menos 2 opciones de respuesta.");
        }

        if (request.Opciones.Any(o => string.IsNullOrWhiteSpace(o.TextoOpcion)))
        {
            return Result<bool>.BadRequest("Todas las opciones agregadas deben tener un texto válido.");
        }

        if (request.Opciones.Count(o => o.EsCorrecta) != 1)
        {
            return Result<bool>.BadRequest("Debe haber exactamente una opción marcada como respuesta correcta.");
        }

        try
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                var pregunta = await _db.PreguntasLecciones
                    .Include(p => p.Opciones)
                    .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

                if (pregunta == null) return Result<bool>.NotFound("Pregunta no encontrada.");

                pregunta.TextoPregunta = request.TextoPregunta.Trim();
                pregunta.ContenidoExplicativo = request.ContenidoExplicativo?.Trim();
                pregunta.Orden = request.Orden <= 0 ? 1 : request.Orden;

                // Remover opciones anteriores
                var opcionesAntiguas = await _db.OpcionesPregunta
                    .Where(o => o.PreguntaId == request.Id)
                    .ToListAsync(cancellationToken);

                if (opcionesAntiguas.Any())
                {
                    _db.OpcionesPregunta.RemoveRange(opcionesAntiguas);
                }

                // Insertar nuevas opciones
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
                return Result<bool>.Success(true);
            });
        }
        catch (Exception ex)
        {
            return Result<bool>.BadRequest($"Error al actualizar la pregunta: {ex.Message}");
        }
    }
}
