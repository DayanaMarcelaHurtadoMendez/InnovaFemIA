using InnovaFemIA.Web.Features.Mapa.ObtenerFamilias;
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
        var result = await _mediator.Send(new ObtenerFamiliasQuery(busqueda, cultivoId));
        return View("~/Views/Mapa/Index.cshtml", result.Value);
    }

    /// <summary>Pantalla 2 — Mapa geográfico interactivo con Leaflet.js.</summary>
    [HttpGet("mapa")]
    public IActionResult Mapa()
    {
        return View("~/Views/Mapa/Mapa.cshtml");
    }

    /// <summary>Pantalla 3 — Precios de mercado del DANE (acceso público).</summary>
    [HttpGet("precios")]
    public IActionResult Precios()
    {
        return View("~/Views/Mapa/Precios.cshtml");
    }
}
