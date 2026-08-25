using InnovaFemIA.Web.Features.Mapa.ObtenerCultivos;
using InnovaFemIA.Web.Features.Mapa.ObtenerFamilias;
using InnovaFemIA.Web.Features.Mapa.ObtenerPrecios;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnovaFemIA.Web.Features.Mapa.Controllers;

/// <summary>
/// Controller MVC para las vistas Razor del Módulo 1 (Mapa).
/// Sirve páginas HTML completas con TailwindCSS.
/// Acceso público, sin autenticación requerida.
/// </summary>
[Route("mapa")]
public class MapaController : Controller
{
    private readonly IMediator _mediator;

    public MapaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Pantalla 1 — Directorio de familias.</summary>
    [HttpGet("")]
    [HttpGet("directorio")]
    public async Task<IActionResult> Index(string? busqueda, int? cultivoId)
    {
        var familiasResult = await _mediator.Send(new ObtenerFamiliasQuery(busqueda, cultivoId));
        var cultivosResult = await _mediator.Send(new ObtenerCultivosQuery());

        ViewBag.Cultivos = cultivosResult.IsSuccess ? cultivosResult.Value : [];
        ViewBag.Busqueda = busqueda;
        ViewBag.CultivoIdSeleccionado = cultivoId;

        return View("~/Views/Mapa/Index.cshtml", familiasResult.Value);
    }

    /// <summary>Pantalla 2 — Mapa geográfico interactivo con Leaflet.js centrado en Caldono, Cauca.</summary>
    [HttpGet("mapa")]
    public async Task<IActionResult> Mapa()
    {
        var cultivosResult = await _mediator.Send(new ObtenerCultivosQuery());
        ViewBag.Cultivos = cultivosResult.IsSuccess ? cultivosResult.Value : [];
        return View("~/Views/Mapa/Mapa.cshtml");
    }

    /// <summary>Pantalla 3 — Precios de mercado del DANE SIPSA (acceso público).</summary>
    [HttpGet("precios")]
    public async Task<IActionResult> Precios(
        [FromQuery] string? grupo = null,
        [FromQuery] string? busqueda = null,
        [FromQuery] string? mercado = null)
    {
        var result = await _mediator.Send(new ObtenerPreciosQuery(grupo, busqueda, mercado));
        ViewBag.GrupoSeleccionado = grupo;
        ViewBag.Busqueda = busqueda;
        ViewBag.MercadoSeleccionado = mercado;

        return View("~/Views/Mapa/Precios.cshtml", result.Value);
    }

    /// <summary>Endpoint para forzar la sincronización manual de precios DANE.</summary>
    [HttpPost("precios/sincronizar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SincronizarPrecios(
        [FromServices] BackgroundServices.ScrapingPreciosService scrapingService)
    {
        var exito = await scrapingService.EjecutarScrapingAsync(HttpContext.RequestAborted);
        TempData[exito ? "MensajeExito" : "MensajeError"] = exito
            ? "Sincronización con DANE SIPSA completada con éxito."
            : "No se pudo sincronizar en vivo con el DANE. Se mantuvieron los datos oficiales de contingencia.";

        return RedirectToAction(nameof(Precios));
    }
}
