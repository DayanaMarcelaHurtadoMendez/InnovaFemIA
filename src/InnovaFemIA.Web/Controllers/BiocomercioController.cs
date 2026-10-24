using System.Security.Claims;
using InnovaFemIA.Web.Features.Biocomercio.Auth;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using InnovaFemIA.Web.Features.Biocomercio.DeteccionEnfermedades;

namespace InnovaFemIA.Web.Controllers;

[Route("biocomercio")]
public class BiocomercioController : Controller
{
    private readonly IMediator _mediator;
    private readonly IRoboflowEnfermedadesService _roboflowService;

    public BiocomercioController(
        IMediator mediator,
        IRoboflowEnfermedadesService roboflowService)
    {
        _mediator = mediator;
        _roboflowService = roboflowService;
    }

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true && User.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "Familia"))
        {
            return RedirectToAction("Index");
        }

        ViewBag.ReturnUrl = returnUrl;
        return View("~/Views/Biocomercio/Login.cshtml");
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Login(string usuario, string contrasena, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(contrasena))
        {
            ViewBag.Error = "Usuario y contraseña son requeridos.";
            ViewBag.ReturnUrl = returnUrl;
            return View("~/Views/Biocomercio/Login.cshtml");
        }

        var result = await _mediator.Send(new AutenticarFamiliaQuery(usuario, contrasena));

        if (!result.IsSuccess)
        {
            ViewBag.Error = result.Error;
            ViewBag.ReturnUrl = returnUrl;
            return View("~/Views/Biocomercio/Login.cshtml");
        }

        var familia = result.Value!;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, familia.Id.ToString()),
            new(ClaimTypes.Name, familia.NombreUsuario),
            new(ClaimTypes.Role, "Familia"),
            new("FamiliaId", familia.FamiliaId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "InnovaFemIA.Familia");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("InnovaFemIA.Familia", principal, new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
        });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index");
    }

    [HttpGet("logout")]
    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = "InnovaFemIA.Familia")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("InnovaFemIA.Familia");
        return RedirectToAction("Login");
    }

    [HttpGet("acceso-denegado")]
    [AllowAnonymous]
    public IActionResult AccesoDenegado()
    {
        return View("~/Views/Biocomercio/AccesoDenegado.cshtml");
    }

    [HttpGet("")]
    [HttpGet("index")]
    [Authorize(AuthenticationSchemes = "InnovaFemIA.Familia")]
    public async Task<IActionResult> Index()
    {
        var preciosResult = await _mediator.Send(new InnovaFemIA.Web.Features.Mapa.ObtenerPrecios.ObtenerPreciosQuery(null, null, null));
        var precios = preciosResult.IsSuccess ? preciosResult.Value : new InnovaFemIA.Web.Features.Mapa.ObtenerPrecios.PreciosSipsaViewModel();
        return View("~/Views/Biocomercio/Index.cshtml", precios);
    }

    [HttpGet("calculadoras")]
    [Authorize(AuthenticationSchemes = "InnovaFemIA.Familia")]
    public async Task<IActionResult> Calculadoras()
    {
        var result = await _mediator.Send(new InnovaFemIA.Web.Features.Biocomercio.Calculadoras.ObtenerTiemposCosechaQuery());
        var tiempos = result.IsSuccess ? result.Value : Enumerable.Empty<InnovaFemIA.Web.Features.Biocomercio.Calculadoras.CultivoTiempoDto>();
        return View("~/Views/Biocomercio/Calculadoras.cshtml", tiempos);
    }

    [HttpGet("mi-stock")]
    [Authorize(AuthenticationSchemes = "InnovaFemIA.Familia")]
    public async Task<IActionResult> MiStock()
    {
        int familiaId = int.Parse(User.FindFirst("FamiliaId")!.Value);
        var result = await _mediator.Send(new InnovaFemIA.Web.Features.Biocomercio.Stock.ObtenerStockFamiliaQuery(familiaId));
        
        if (!result.IsSuccess)
        {
            return RedirectToAction("Index");
        }

        return View("~/Views/Biocomercio/MiStock.cshtml", result.Value);
    }

    [HttpPost("mi-stock")]
    [Authorize(AuthenticationSchemes = "InnovaFemIA.Familia")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarStock(List<int> cultivosDisponibles)
    {
        int familiaId = int.Parse(User.FindFirst("FamiliaId")!.Value);
        var result = await _mediator.Send(new InnovaFemIA.Web.Features.Biocomercio.Stock.ActualizarStockFamiliaCommand(familiaId, cultivosDisponibles ?? new List<int>()));

        if (result.IsSuccess)
        {
            TempData["Exito"] = "Tu stock está actualizado";
        }
        else
        {
            TempData["Error"] = "Ocurrió un error al actualizar el stock";
        }

        return RedirectToAction("MiStock");
    }

    [HttpGet("deteccion-enfermedades")]
    [AllowAnonymous]
    public IActionResult DeteccionEnfermedades()
    {
        return View("~/Views/Biocomercio/DeteccionEnfermedades.cshtml");
    }

    [HttpPost("api/detectar-enfermedad")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> DetectarEnfermedad([FromBody] DeteccionRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ImagenBase64))
        {
            return BadRequest(new ResultadoDeteccionDto
            {
                Exito = false,
                Mensaje = "No se recibió ninguna imagen para analizar."
            });
        }

        var resultado = await _roboflowService.AnalizarImagenAsync(request.ImagenBase64, request.NombreArchivo);
        return Ok(resultado);
    }
}
