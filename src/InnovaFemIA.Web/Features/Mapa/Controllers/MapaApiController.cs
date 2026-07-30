using InnovaFemIA.Web.Features.Mapa.ObtenerActores;
using InnovaFemIA.Web.Features.Mapa.ObtenerCultivos;
using InnovaFemIA.Web.Features.Mapa.ObtenerDetalleFamilia;
using InnovaFemIA.Web.Features.Mapa.ObtenerFamilias;
using InnovaFemIA.Web.Features.Mapa.ObtenerPrecios;
using InnovaFemIA.Web.Features.Shared.Attributes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnovaFemIA.Web.Features.Mapa.Controllers;

/// <summary>
/// Controller de API REST para el Módulo 1 (Mapa).
/// Los endpoints SIN [PublicApi] son de uso interno (el frontend Razor los consume via fetch/ajax).
/// Los endpoints CON [PublicApi] son accesibles externamente con API Key válida.
/// </summary>
[ApiController]
[Route("api/mapa")]
[Produces("application/json")]
public class MapaApiController : ControllerBase
{
    private readonly IMediator _mediator;

    public MapaApiController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Obtiene el listado de familias con sus cultivos y posición GPS.
    /// [PublicApi] → accesible externamente con API Key.
    /// </summary>
    [HttpGet("familias")]
    [PublicApi]
    public async Task<IActionResult> ObtenerFamilias(
        [FromQuery] string? busqueda = null,
        [FromQuery] int? cultivoId = null)
    {
        var result = await _mediator.Send(new ObtenerFamiliasQuery(busqueda, cultivoId));
        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Obtiene el detalle y ficha técnica de una familia específica por ID.
    /// </summary>
    [HttpGet("familias/{id:int}")]
    public async Task<IActionResult> ObtenerDetalleFamilia(int id)
    {
        var result = await _mediator.Send(new ObtenerDetalleFamiliaQuery(id));
        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Obtiene los actores del mapa (asociaciones y puntos de compra).
    /// Sin [PublicApi] → solo uso interno para el mapa interactivo.
    /// </summary>
    [HttpGet("actores")]
    public async Task<IActionResult> ObtenerActores([FromQuery] string? tipo = null)
    {
        var result = await _mediator.Send(new ObtenerActoresQuery(tipo));
        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Obtiene el listado de cultivos activos del sistema.
    /// </summary>
    [HttpGet("cultivos")]
    public async Task<IActionResult> ObtenerCultivos()
    {
        var result = await _mediator.Send(new ObtenerCultivosQuery());
        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Obtiene los precios de mercado vigentes del DANE.
    /// [PublicApi] → accesible externamente.
    /// </summary>
    [HttpGet("precios")]
    [PublicApi]
    public async Task<IActionResult> ObtenerPrecios()
    {
        var result = await _mediator.Send(new ObtenerPreciosQuery());
        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }
}
