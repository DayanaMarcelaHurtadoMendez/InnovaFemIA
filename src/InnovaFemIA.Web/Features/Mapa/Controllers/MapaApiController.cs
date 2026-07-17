using InnovaFemIA.Web.Features.Mapa.ObtenerFamilias;
using InnovaFemIA.Web.Features.Shared.Attributes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnovaFemIA.Web.Features.Mapa.Controllers;

/// <summary>
/// Controller de API REST para el Módulo 1 (Mapa).
/// Los endpoints SIN [PublicApi] son de uso interno (el frontend Razor los consume via fetch/ajax).
/// Los endpoints CON [PublicApi] son accesibles externamente con API Key válida.
///
/// El middleware ApiKeyMiddleware controla el acceso basado en la presencia del atributo.
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
    /// Obtiene el listado de familias con sus cultivos y stock.
    /// [PublicApi] → accesible externamente con API Key (útil para integraciones de terceros).
    /// </summary>
    [HttpGet("familias")]
    [PublicApi] // Con este atributo, el middleware permite acceso externo (requiere X-Api-Key)
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
    /// Obtiene los actores del mapa (asociaciones y puntos de compra).
    /// Sin [PublicApi] → solo acceso interno (consumido por mapa.js en el browser).
    /// Cualquier petición directa retorna 403 Forbidden.
    /// </summary>
    [HttpGet("actores")]
    // Sin [PublicApi] → el middleware retorna 403 si se accede externamente
    public async Task<IActionResult> ObtenerActores([FromQuery] string? tipo = null)
    {
        // TODO: Implementar ObtenerActoresQuery en Fase 2
        return Ok(new { mensaje = "Endpoint actores - pendiente implementación" });
    }

    /// <summary>
    /// Obtiene la fase lunar actual y las prácticas recomendadas.
    /// Sin [PublicApi] → solo uso interno (botón lunita en el frontend).
    /// </summary>
    [HttpGet("luna")]
    public async Task<IActionResult> ObtenerFaseLunar()
    {
        // TODO: Implementar ObtenerFaseLunarHandler en Fase 2
        return Ok(new { mensaje = "Endpoint lunar - pendiente implementación" });
    }

    /// <summary>
    /// Obtiene los precios de mercado actuales del DANE.
    /// [PublicApi] → accesible externamente (útil para aplicaciones de la comunidad).
    /// </summary>
    [HttpGet("precios")]
    [PublicApi]
    public async Task<IActionResult> ObtenerPrecios()
    {
        // TODO: Implementar ObtenerPreciosHandler en Fase 2
        return Ok(new { mensaje = "Endpoint precios - pendiente implementación" });
    }
}
