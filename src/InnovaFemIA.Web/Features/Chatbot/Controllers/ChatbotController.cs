using InnovaFemIA.Web.Features.Shared.Entities;
using InnovaFemIA.Web.Features.Shared.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnovaFemIA.Web.Features.Chatbot.Controllers;

/// <summary>
/// Controller público para el Módulo 2 — Asistente Educativo (Chatbot y Modo Lección).
/// Acceso 100% público (sin login requerido).
/// </summary>
[Route("chatbot")]
public class ChatbotController : Controller
{
    private readonly IMediator _mediator;
    private readonly IMediaCacheService _mediaCacheService;

    public ChatbotController(IMediator mediator, IMediaCacheService mediaCacheService)
    {
        _mediator = mediator;
        _mediaCacheService = mediaCacheService;
    }

    /// <summary>Pantalla 1 — Interfaz principal del Chatbot Educativo Amalia.</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var cultivosResult = await _mediator.Send(new ObtenerCultivosChatbotQuery());
        var leccionesResult = await _mediator.Send(new ObtenerLeccionesEducativasQuery());

        ViewBag.Cultivos = cultivosResult.IsSuccess ? cultivosResult.Value : [];
        ViewBag.Lecciones = leccionesResult.IsSuccess ? leccionesResult.Value : [];

        return View("~/Views/Chatbot/Index.cshtml");
    }

    /// <summary>Pantalla 2 — Catálogo del Modo Lección.</summary>
    [HttpGet("lecciones")]
    public async Task<IActionResult> Lecciones()
    {
        var leccionesResult = await _mediator.Send(new ObtenerLeccionesEducativasQuery());
        return View("~/Views/Chatbot/Lecciones.cshtml", leccionesResult.IsSuccess ? leccionesResult.Value : []);
    }

    /// <summary>Pantalla 3 — Reproductor interactivo de preguntas y respuestas.</summary>
    [HttpGet("lecciones/{id:int}")]
    public async Task<IActionResult> LeccionDetalle(int id)
    {
        var result = await _mediator.Send(new ObtenerDetalleLeccionQuery(id));
        if (!result.IsSuccess || result.Value == null)
        {
            TempData["Error"] = "La lección solicitada no existe o no está disponible.";
            return RedirectToAction("Lecciones");
        }

        return View("~/Views/Chatbot/LeccionDetalle.cshtml", result.Value);
    }

    // ─── Endpoints API JSON para el flujo dinámico del Chatbot ─────────────

    [HttpGet("api/cultivos")]
    public async Task<IActionResult> ApiCultivos()
    {
        var result = await _mediator.Send(new ObtenerCultivosChatbotQuery());
        return Json(result.IsSuccess ? result.Value : []);
    }

    [HttpGet("api/temas-por-cultivo")]
    public async Task<IActionResult> ApiTemasPorCultivo([FromQuery] int cultivoId)
    {
        var result = await _mediator.Send(new ObtenerTemasPorCultivoQuery(cultivoId));
        return Json(result.IsSuccess ? result.Value : []);
    }

    [HttpGet("api/contenidos")]
    public async Task<IActionResult> ApiContenidos([FromQuery] int cultivoId, [FromQuery] TemaContenido tema)
    {
        var result = await _mediator.Send(new ObtenerContenidosPorTemaQuery(cultivoId, tema));
        return Json(result.IsSuccess ? result.Value : []);
    }

    [HttpGet("api/recurso/{id:int}")]
    public async Task<IActionResult> ApiRecurso(int id)
    {
        var result = await _mediator.Send(new ObtenerDetalleRecursoQuery(id));
        if (!result.IsSuccess) return NotFound(new { error = result.Error });
        return Json(result.Value);
    }

    // ─── Servir Archivos e Imágenes con Streaming por Rangos HTTP ──────────

    [HttpGet("recursos/{id:int}/archivo")]
    public async Task<IActionResult> ServirArchivoRecurso(int id)
    {
        var medio = await _mediaCacheService.ObtenerRutaMedioAsync(id);
        if (medio == null || string.IsNullOrEmpty(medio.Value.FilePath))
        {
            return NotFound("El archivo no se encuentra disponible.");
        }

        Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
        Response.Headers["Accept-Ranges"] = "bytes";

        // Kernel-mode zero-copy streaming con PhysicalFileResult (0% RAM / 0% CPU)
        return PhysicalFile(medio.Value.FilePath, medio.Value.MimeType, enableRangeProcessing: true);
    }

    [HttpGet("lecciones/{id:int}/imagen")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> ServirImagenLeccion(int id)
    {
        var result = await _mediator.Send(new ObtenerImagenLeccionQuery(id));
        if (!result.IsSuccess || result.Value == null || result.Value.Bytes.Length == 0)
        {
            return NotFound();
        }

        return File(result.Value.Bytes, result.Value.MimeType);
    }

    [HttpGet("preguntas/{id:int}/imagen")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> ServirImagenPregunta(int id)
    {
        var result = await _mediator.Send(new ObtenerImagenPreguntaQuery(id));
        if (!result.IsSuccess || result.Value == null || result.Value.Bytes.Length == 0)
        {
            return NotFound();
        }

        return File(result.Value.Bytes, result.Value.MimeType);
    }
}
