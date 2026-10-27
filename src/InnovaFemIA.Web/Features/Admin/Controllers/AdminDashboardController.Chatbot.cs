using InnovaFemIA.Web.Features.Admin.GestionContenidosChatbot;
using InnovaFemIA.Web.Features.Admin.GestionLeccionesChatbot;
using InnovaFemIA.Web.Features.Chatbot;
using InnovaFemIA.Web.Features.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InnovaFemIA.Web.Features.Admin.Controllers;

public partial class AdminDashboardController
{
    // ── MÓDULO 2: GESTIÓN DE CONTENIDOS EDUCATIVOS CHATBOT ────────────────────
    [HttpGet("chatbot/contenidos")]
    public async Task<IActionResult> ChatbotContenidos(int? cultivoId, TemaContenido? tema)
    {
        var result = await _mediator.Send(new ObtenerContenidosChatbotQuery(cultivoId, tema));
        var cultivosResult = await _mediator.Send(new Mapa.ObtenerCultivos.ObtenerCultivosQuery());
        
        ViewBag.Cultivos = cultivosResult.Value ?? [];
        ViewBag.CultivoSeleccionado = cultivoId;
        ViewBag.TemaSeleccionado = tema;
        return View("~/Views/Admin/ChatbotContenidos.cshtml", result.Value);
    }

    [HttpGet("chatbot/contenidos/{id:int}/archivo")]
    [AllowAnonymous] // Permite que el chatbot y las vistas muestren el archivo sin requerir sesión admin
    public async Task<IActionResult> ServirArchivoContenido(int id)
    {
        try
        {
            var medio = await _mediaCacheService.ObtenerRutaMedioAsync(id);
            if (medio != null && !string.IsNullOrEmpty(medio.Value.FilePath) && System.IO.File.Exists(medio.Value.FilePath))
            {
                Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
                Response.Headers["Accept-Ranges"] = "bytes";
                return PhysicalFile(medio.Value.FilePath, medio.Value.MimeType, enableRangeProcessing: true);
            }
        }
        catch
        {
            // Continuar con fallback a BD
        }

        var archivoDb = await _mediator.Send(new ObtenerArchivoRecursoQuery(id));
        if (archivoDb.IsSuccess && archivoDb.Value != null && archivoDb.Value.Bytes.Length > 0)
        {
            Response.Headers["Cache-Control"] = "public, max-age=86400";
            Response.Headers["Accept-Ranges"] = "bytes";
            return File(archivoDb.Value.Bytes, archivoDb.Value.MimeType, archivoDb.Value.NombreArchivo, enableRangeProcessing: true);
        }

        return NotFound("El archivo no se encuentra disponible.");
    }

    [HttpPost("chatbot/contenidos/crear")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(500 * 1024 * 1024)] // 500 MB max HTTP request size
    [RequestFormLimits(MultipartBodyLengthLimit = 500 * 1024 * 1024)]
    public async Task<IActionResult> CrearContenidoChatbot([FromForm] int cultivoId, [FromForm] TemaContenido tema, [FromForm] string titulo, [FromForm] string? url, [FromForm] int orden, [FromForm] string? nombreFuente, [FromForm] string? enlaceFuente, IFormFile? archivo)
    {
        try
        {
            byte[]? archivoBytes = null;
            string? mimeType = null;
            string? archivoNombre = null;
            string? finalUrl = url?.Trim();

            if (archivo != null && archivo.Length > 0)
            {
                double tamanoMb = archivo.Length / (1024.0 * 1024.0);

                if (archivo.Length > 300 * 1024 * 1024)
                {
                    TempData["Error"] = $"El archivo cargado pesa {tamanoMb:F1} MB. El límite máximo admitido es de 300 MB por archivo. Para videos de mayor peso, por favor utiliza un enlace web (YouTube / Google Drive / Vimeo).";
                    return RedirectToAction("ChatbotContenidos");
                }

                archivoNombre = Path.GetFileName(archivo.FileName);
                mimeType = archivo.ContentType;

                using (var ms = new MemoryStream())
                {
                    await archivo.CopyToAsync(ms);
                    archivoBytes = ms.ToArray();
                }
            }

            var command = new CrearContenidoChatbotCommand(cultivoId, tema, titulo, finalUrl, orden, archivoBytes, mimeType, archivoNombre, nombreFuente, enlaceFuente);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Error;
            }
            else
            {
                // Guardar en la caché local persistente del servidor inmediatamente
                if (archivoBytes != null && archivoBytes.Length > 0)
                {
                    await _mediaCacheService.GuardarEnCacheAsync(result.Value, archivoBytes, mimeType, archivoNombre);
                }

                TempData["Exito"] = "Recurso educativo registrado exitosamente.";
            }
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            TempData["Error"] = "No se pudo guardar el archivo cargado. Por favor verifica que el archivo no supere los 300 MB o intenta con un archivo más ligero.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Ocurrió un inconveniente al procesar la solicitud: {ex.Message}";
        }

        return RedirectToAction("ChatbotContenidos");
    }

    [HttpPost("chatbot/contenidos/editar")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(500 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 500 * 1024 * 1024)]
    public async Task<IActionResult> EditarContenidoChatbot(
        [FromForm] int id,
        [FromForm] int cultivoId,
        [FromForm] TemaContenido tema,
        [FromForm] string titulo,
        [FromForm] string? url,
        [FromForm] int orden,
        [FromForm] bool activo,
        [FromForm] string? nombreFuente,
        [FromForm] string? enlaceFuente,
        IFormFile? archivo)
    {
        try
        {
            byte[]? archivoBytes = null;
            string? mimeType = null;
            string? archivoNombre = null;
            string? finalUrl = url?.Trim();

            if (archivo != null && archivo.Length > 0)
            {
                if (archivo.Length > 300 * 1024 * 1024)
                {
                    TempData["Error"] = "El archivo cargado supera el límite máximo de 300 MB.";
                    return RedirectToAction("ChatbotContenidos");
                }

                archivoNombre = Path.GetFileName(archivo.FileName);
                mimeType = archivo.ContentType;
                using var ms = new MemoryStream();
                await archivo.CopyToAsync(ms);
                archivoBytes = ms.ToArray();
            }

            var command = new EditarContenidoChatbotCommand(
                id, cultivoId, tema, titulo, finalUrl, orden, activo, archivoBytes, mimeType, archivoNombre, nombreFuente, enlaceFuente);

            var result = await _mediator.Send(command);
            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Error;
            }
            else
            {
                // Actualizar caché física local en el servidor
                if (archivoBytes != null && archivoBytes.Length > 0)
                {
                    await _mediaCacheService.GuardarEnCacheAsync(id, archivoBytes, mimeType, archivoNombre);
                }

                TempData["Exito"] = "Recurso educativo actualizado exitosamente.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Ocurrió un inconveniente al actualizar el recurso: {ex.Message}";
        }

        return RedirectToAction("ChatbotContenidos");
    }

    [HttpPost("chatbot/contenidos/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarContenidoChatbot([FromForm] int id)
    {
        var result = await _mediator.Send(new EliminarContenidoChatbotCommand(id));
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
        }
        else
        {
            _mediaCacheService.InvalidarCache(id);
            TempData["Exito"] = "Recurso educativo eliminado exitosamente.";
        }
        return RedirectToAction("ChatbotContenidos");
    }

    // ── MÓDULO 2: GESTIÓN DE LECCIONES & PREGUNTAS MODO DUOLINGO ──────────────
    [HttpGet("chatbot/lecciones")]
    public async Task<IActionResult> ChatbotLecciones()
    {
        var result = await _mediator.Send(new ObtenerLeccionesAdminQuery());
        return View("~/Views/Admin/ChatbotLecciones.cshtml", result.Value);
    }

    [HttpPost("chatbot/lecciones/crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearLeccion([FromForm] CrearLeccionCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Lección creada exitosamente.";
        return RedirectToAction("ChatbotLecciones");
    }

    [HttpPost("chatbot/lecciones/agregar-pregunta")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarPreguntaLeccion([FromForm] int leccionId, [FromForm] string textoPregunta, [FromForm] string? contenidoExplicativo, [FromForm] int orden, [FromForm] List<string> opcionTexto, [FromForm] int opcionCorrectaIndex, [FromForm] List<string> opcionRetro)
    {
        try
        {
            var opciones = new List<OpcionPreguntaInput>();
            for (int i = 0; i < opcionTexto.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(opcionTexto[i]))
                {
                    opciones.Add(new OpcionPreguntaInput(
                        opcionTexto[i].Trim(),
                        i == opcionCorrectaIndex,
                        i < opcionRetro.Count ? opcionRetro[i]?.Trim() : null
                    ));
                }
            }

            var command = new AgregarPreguntaLeccionCommand(leccionId, textoPregunta, contenidoExplicativo, orden, opciones);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess) TempData["Error"] = result.Error;
            else TempData["Exito"] = "Pregunta agregada a la lección exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Ocurrió un error al agregar la pregunta: {ex.Message}";
        }

        return RedirectToAction("ChatbotLecciones");
    }

    [HttpPost("chatbot/lecciones/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarLeccion([FromForm] EditarLeccionCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            if (!result.IsSuccess) TempData["Error"] = result.Error;
            else TempData["Exito"] = "Lección actualizada exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Ocurrió un error al actualizar la lección: {ex.Message}";
        }
        return RedirectToAction("ChatbotLecciones");
    }

    [HttpPost("chatbot/lecciones/editar-pregunta")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarPregunta([FromForm] int id, [FromForm] string textoPregunta, [FromForm] string? contenidoExplicativo, [FromForm] int orden, [FromForm] List<string> opcionTexto, [FromForm] int opcionCorrectaIndex, [FromForm] List<string> opcionRetro)
    {
        try
        {
            var opciones = new List<OpcionPreguntaInput>();
            for (int i = 0; i < opcionTexto.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(opcionTexto[i]))
                {
                    opciones.Add(new OpcionPreguntaInput(
                        opcionTexto[i].Trim(),
                        i == opcionCorrectaIndex,
                        i < opcionRetro.Count ? opcionRetro[i]?.Trim() : null
                    ));
                }
            }

            var command = new EditarPreguntaLeccionCommand(id, textoPregunta, contenidoExplicativo, orden, opciones);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess) TempData["Error"] = result.Error;
            else TempData["Exito"] = "Pregunta y sus opciones actualizadas exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Ocurrió un error al actualizar la pregunta: {ex.Message}";
        }
        return RedirectToAction("ChatbotLecciones");
    }

    [HttpPost("chatbot/lecciones/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarLeccion([FromForm] int id)
    {
        try
        {
            var result = await _mediator.Send(new EliminarLeccionCommand(id));
            if (!result.IsSuccess) TempData["Error"] = result.Error;
            else TempData["Exito"] = "Lección y sus preguntas eliminadas exitosamente en cascada.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Ocurrió un error al eliminar la lección: {ex.Message}";
        }
        return RedirectToAction("ChatbotLecciones");
    }

    [HttpPost("chatbot/lecciones/eliminar-pregunta")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarPregunta([FromForm] int id)
    {
        try
        {
            var result = await _mediator.Send(new EliminarPreguntaCommand(id));
            if (!result.IsSuccess) TempData["Error"] = result.Error;
            else TempData["Exito"] = "Pregunta eliminada exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Ocurrió un error al eliminar la pregunta: {ex.Message}";
        }
        return RedirectToAction("ChatbotLecciones");
    }
}
