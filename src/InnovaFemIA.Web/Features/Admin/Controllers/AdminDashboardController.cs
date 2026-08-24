using InnovaFemIA.Web.Features.Admin.AdminDashboard;
using InnovaFemIA.Web.Features.Admin.GestionActores;
using InnovaFemIA.Web.Features.Admin.GestionContenidosChatbot;
using InnovaFemIA.Web.Features.Admin.GestionCuentasFamilias;
using InnovaFemIA.Web.Features.Admin.GestionFamilias;
using InnovaFemIA.Web.Features.Admin.GestionLeccionesChatbot;
using InnovaFemIA.Web.Features.Admin.GestionReglasLunares;
using InnovaFemIA.Web.Features.Admin.GestionRegistros;
using InnovaFemIA.Web.Features.Admin.GestionStockConsolidado;
using InnovaFemIA.Web.Features.Admin.GestionTiemposCosecha;
using InnovaFemIA.Web.Features.Shared.Entities;
using InnovaFemIA.Web.Features.Shared.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InnovaFemIA.Web.Features.Admin.Controllers;

[Route("admin")]
[Authorize(AuthenticationSchemes = "InnovaFemIA.Admin", Roles = "Administrador")]
public class AdminDashboardController : Controller
{
    private readonly IMediator _mediator;
    private readonly IMediaCacheService _mediaCacheService;

    public AdminDashboardController(IMediator mediator, IMediaCacheService mediaCacheService)
    {
        _mediator = mediator;
        _mediaCacheService = mediaCacheService;
    }

    // ── DASHBOARD PRINCIPAL ──────────────────────────────────────────────────
    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var result = await _mediator.Send(new ObtenerDashboardQuery());
        return View("~/Views/Admin/Dashboard.cshtml", result.Value);
    }

    // ── MÓDULO 1: GESTIÓN DE FAMILIAS ASCAMPESINA ────────────────────────────
    [HttpGet("familias")]
    public async Task<IActionResult> Familias(string? busqueda)
    {
        var result = await _mediator.Send(new ObtenerFamiliasAdminQuery(busqueda));
        var cultivosResult = await _mediator.Send(new Mapa.ObtenerCultivos.ObtenerCultivosQuery());
        
        ViewBag.Cultivos = cultivosResult.Value ?? [];
        ViewBag.Busqueda = busqueda;
        return View("~/Views/Admin/Familias.cshtml", result.Value);
    }

    [HttpPost("familias/crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearFamilia(
        [FromForm] string nombre,
        [FromForm] string vereda,
        [FromForm] string municipio,
        [FromForm] string nombreContacto,
        [FromForm] string telefonoContacto,
        [FromForm] double latitud,
        [FromForm] double longitud,
        [FromForm] string? descripcion,
        [FromForm] List<int>? cultivosIds,
        IFormFile? foto)
    {
        byte[]? fotoBytes = null;
        string? fotoMimeType = null;

        if (foto != null && foto.Length > 0)
        {
            using var ms = new MemoryStream();
            await foto.CopyToAsync(ms);
            fotoBytes = ms.ToArray();
            fotoMimeType = foto.ContentType;
        }

        var command = new CrearFamiliaAdminCommand(
            nombre,
            vereda,
            municipio,
            nombreContacto,
            telefonoContacto,
            latitud,
            longitud,
            descripcion,
            cultivosIds ?? [],
            fotoBytes,
            fotoMimeType
        );

        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Familia creada exitosamente.";
        return RedirectToAction("Familias");
    }

    [HttpPost("familias/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarFamilia(
        [FromForm] int id,
        [FromForm] string nombre,
        [FromForm] string vereda,
        [FromForm] string municipio,
        [FromForm] string nombreContacto,
        [FromForm] string telefonoContacto,
        [FromForm] double latitud,
        [FromForm] double longitud,
        [FromForm] string? descripcion,
        [FromForm] bool activo,
        [FromForm] List<int>? cultivosIds,
        IFormFile? foto)
    {
        byte[]? fotoBytes = null;
        string? fotoMimeType = null;

        if (foto != null && foto.Length > 0)
        {
            using var ms = new MemoryStream();
            await foto.CopyToAsync(ms);
            fotoBytes = ms.ToArray();
            fotoMimeType = foto.ContentType;
        }

        var command = new EditarFamiliaAdminCommand(
            id,
            nombre,
            vereda,
            municipio,
            nombreContacto,
            telefonoContacto,
            latitud,
            longitud,
            descripcion,
            activo,
            cultivosIds ?? [],
            fotoBytes,
            fotoMimeType
        );

        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Familia actualizada exitosamente.";
        return RedirectToAction("Familias");
    }

    [HttpPost("familias/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarFamilia([FromForm] int id)
    {
        var result = await _mediator.Send(new EliminarFamiliaAdminCommand(id));
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Familia eliminada exitosamente.";
        return RedirectToAction("Familias");
    }

    // ── MÓDULO 1: GESTIÓN DE ACTORES DEL MAPA ────────────────────────────────
    [HttpGet("actores")]
    public async Task<IActionResult> Actores(TipoActorMapa? tipo)
    {
        var result = await _mediator.Send(new ObtenerActoresAdminQuery(tipo));
        ViewBag.TipoSeleccionado = tipo;
        return View("~/Views/Admin/Actores.cshtml", result.Value);
    }

    [HttpPost("actores/crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearActor([FromForm] CrearActorAdminCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Actor del mapa registrado exitosamente.";
        return RedirectToAction("Actores");
    }

    [HttpPost("actores/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarActor([FromForm] EditarActorAdminCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Actor del mapa actualizado exitosamente.";
        return RedirectToAction("Actores");
    }

    [HttpPost("actores/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarActor([FromForm] int id)
    {
        var result = await _mediator.Send(new EliminarActorAdminCommand(id));
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Actor eliminado exitosamente.";
        return RedirectToAction("Actores");
    }

    // ── MÓDULO 1: GESTIÓN DE REGLAS Y PRÁCTICAS LUNARES ──────────────────────
    [HttpGet("reglas-lunares")]
    public async Task<IActionResult> ReglasLunares(FaseLunar? fase)
    {
        var result = await _mediator.Send(new ObtenerReglasLunaresAdminQuery(fase));
        var cultivosResult = await _mediator.Send(new Mapa.ObtenerCultivos.ObtenerCultivosQuery());
        
        ViewBag.Cultivos = cultivosResult.Value ?? [];
        ViewBag.FaseSeleccionada = fase;
        return View("~/Views/Admin/ReglasLunares.cshtml", result.Value);
    }

    [HttpPost("reglas-lunares/crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearReglaLunar([FromForm] CrearReglaLunarCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Práctica lunar agregada exitosamente.";
        return RedirectToAction("ReglasLunares");
    }

    [HttpPost("reglas-lunares/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarReglaLunar([FromForm] EditarReglaLunarCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Práctica lunar actualizada exitosamente.";
        return RedirectToAction("ReglasLunares");
    }

    [HttpPost("reglas-lunares/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarReglaLunar([FromForm] int id)
    {
        var result = await _mediator.Send(new EliminarReglaLunarCommand(id));
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Práctica lunar eliminada exitosamente.";
        return RedirectToAction("ReglasLunares");
    }

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
        var medio = await _mediaCacheService.ObtenerRutaMedioAsync(id);
        if (medio == null || string.IsNullOrEmpty(medio.Value.FilePath))
        {
            return NotFound("El archivo no se encuentra disponible.");
        }

        Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
        Response.Headers["Accept-Ranges"] = "bytes";

        return PhysicalFile(medio.Value.FilePath, medio.Value.MimeType, enableRangeProcessing: true);
    }

    // ── GESTIÓN DE CULTIVOS DEL SISTEMA (ADMIN) ──────────────────────────────
    [HttpGet("cultivos")]
    public async Task<IActionResult> Cultivos()
    {
        var result = await _mediator.Send(new GestionCultivos.ObtenerCultivosAdminQuery());
        return View("~/Views/Admin/Cultivos.cshtml", result.Value);
    }

    [HttpPost("cultivos/crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearCultivo([FromForm] GestionCultivos.CrearCultivoCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Cultivo registrado exitosamente.";
        return RedirectToAction("Cultivos");
    }

    [HttpPost("cultivos/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarCultivo([FromForm] GestionCultivos.EditarCultivoCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Cultivo actualizado exitosamente.";
        return RedirectToAction("Cultivos");
    }

    [HttpPost("cultivos/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarCultivo([FromForm] int id)
    {
        var result = await _mediator.Send(new GestionCultivos.EliminarCultivoCommand(id));
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Cultivo eliminado exitosamente.";
        return RedirectToAction("Cultivos");
    }

    [HttpPost("chatbot/contenidos/crear")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(500 * 1024 * 1024)] // 500 MB max HTTP request size
    [RequestFormLimits(MultipartBodyLengthLimit = 500 * 1024 * 1024)]
    public async Task<IActionResult> CrearContenidoChatbot([FromForm] int cultivoId, [FromForm] TemaContenido tema, [FromForm] string titulo, [FromForm] string? url, [FromForm] int orden, IFormFile? archivo)
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

            var command = new CrearContenidoChatbotCommand(cultivoId, tema, titulo, finalUrl, orden, archivoBytes, mimeType, archivoNombre);
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
        IFormFile? archivo)
    {
        try
        {
            byte[]? archivoBytes = null;
            string? mimeType = null;
            string? archivoNombre = null;

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
                id, cultivoId, tema, titulo, url, orden, activo, archivoBytes, mimeType, archivoNombre);

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

        return RedirectToAction("ChatbotLecciones");
    }

    [HttpPost("chatbot/lecciones/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarLeccion([FromForm] int id)
    {
        var result = await _mediator.Send(new EliminarLeccionCommand(id));
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Lección y sus preguntas eliminadas exitosamente en cascada.";
        return RedirectToAction("ChatbotLecciones");
    }

    [HttpPost("chatbot/lecciones/eliminar-pregunta")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarPregunta([FromForm] int id)
    {
        var result = await _mediator.Send(new EliminarPreguntaCommand(id));
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Pregunta eliminada exitosamente.";
        return RedirectToAction("ChatbotLecciones");
    }

    // ── MÓDULO 3: GESTIÓN DE CUENTAS DE FAMILIAS (BIOCOMERCIO) ───────────────
    [HttpGet("biocomercio/cuentas")]
    [HttpGet("cuentas-familias")]
    public async Task<IActionResult> CuentasFamilias()
    {
        var result = await _mediator.Send(new ObtenerCuentasFamiliasQuery());
        var familiasResult = await _mediator.Send(new ObtenerFamiliasAdminQuery());
        
        ViewBag.Familias = familiasResult.Value ?? [];
        return View("~/Views/Admin/CuentasFamilias.cshtml", result.Value);
    }

    [HttpPost("cuentas-familias/crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearCuentaFamilia([FromForm] CrearCuentaFamiliaCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Cuenta de familia creada exitosamente.";
        return RedirectToAction("CuentasFamilias");
    }

    [HttpPost("cuentas-familias/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarCuentaFamilia([FromForm] EditarCuentaFamiliaCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Cuenta de familia actualizada exitosamente.";
        return RedirectToAction("CuentasFamilias");
    }

    [HttpPost("cuentas-familias/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarCuentaFamilia([FromForm] int id)
    {
        var result = await _mediator.Send(new EliminarCuentaFamiliaCommand(id));
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Cuenta de familia eliminada exitosamente.";
        return RedirectToAction("CuentasFamilias");
    }

    // ── MÓDULO 3: VISTA CONSOLIDADA DE STOCK ─────────────────────────────────
    [HttpGet("biocomercio/stock")]
    public async Task<IActionResult> BiocomercioStock()
    {
        var result = await _mediator.Send(new ObtenerStockConsolidadoQuery());
        return View("~/Views/Admin/BiocomercioStock.cshtml", result.Value);
    }

    // ── MÓDULO 3: TIEMPOS ESTÁNDAR DE COSECHA (CALCULADORA) ──────────────────
    [HttpGet("biocomercio/tiempos")]
    public async Task<IActionResult> BiocomercioTiempos()
    {
        var result = await _mediator.Send(new ObtenerTiemposCosechaAdminQuery());
        var cultivosResult = await _mediator.Send(new Mapa.ObtenerCultivos.ObtenerCultivosQuery());
        
        ViewBag.Cultivos = cultivosResult.Value ?? [];
        return View("~/Views/Admin/BiocomercioTiempos.cshtml", result.Value);
    }

    [HttpPost("biocomercio/tiempos/crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearTiempoCosecha([FromForm] CrearTiempoCosechaCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Tiempo estándar de cosecha registrado exitosamente.";
        return RedirectToAction("BiocomercioTiempos");
    }

    [HttpPost("biocomercio/tiempos/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarTiempoCosecha([FromForm] EditarTiempoCosechaCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Tiempo estándar actualizado exitosamente.";
        return RedirectToAction("BiocomercioTiempos");
    }

    [HttpPost("biocomercio/tiempos/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarTiempoCosecha([FromForm] int id)
    {
        var result = await _mediator.Send(new EliminarTiempoCosechaCommand(id));
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Registro eliminado exitosamente.";
        return RedirectToAction("BiocomercioTiempos");
    }

    // ── SISTEMA Y AUDITORÍA: LOGS Y EXCEPCIONES ─────────────────────────────
    [HttpGet("registros")]
    public async Task<IActionResult> Registros(string? modulo, string? tipo)
    {
        var result = await _mediator.Send(new ObtenerRegistrosQuery(modulo, tipo));
        ViewBag.Modulo = modulo;
        ViewBag.Tipo = tipo;
        return View("~/Views/Admin/Registros.cshtml", result.Value);
    }
}
