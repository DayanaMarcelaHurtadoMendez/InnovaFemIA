using System.Security.Claims;
using InnovaFemIA.Web.Features.Admin.AdminLogin;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InnovaFemIA.Web.Features.Admin.Controllers;

[Route("admin")]
public class AdminController : Controller
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        var pathBase = ResolvePathBase(HttpContext);

        if (User.Identity?.IsAuthenticated == true && User.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "Administrador"))
        {
            var targetDashboard = string.IsNullOrEmpty(pathBase) ? "/admin/dashboard" : $"{pathBase}/admin/dashboard";
            return Redirect(targetDashboard);
        }

        ViewBag.ReturnUrl = returnUrl;
        ViewBag.PathBase = pathBase;
        return View("~/Views/Admin/Login.cshtml");
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Login(string usuario, string contrasena, string? returnUrl = null)
    {
        var pathBase = ResolvePathBase(HttpContext);

        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(contrasena))
        {
            ViewBag.Error = "Usuario y contraseña son requeridos.";
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.PathBase = pathBase;
            return View("~/Views/Admin/Login.cshtml");
        }

        var result = await _mediator.Send(new AdminLoginCommand(usuario, contrasena));

        if (!result.IsSuccess)
        {
            ViewBag.Error = result.Error;
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.PathBase = pathBase;
            return View("~/Views/Admin/Login.cshtml");
        }

        var admin = result.Value!;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new(ClaimTypes.Name, admin.NombreUsuario),
            new(ClaimTypes.GivenName, admin.NombreCompleto),
            new(ClaimTypes.Email, admin.Email),
            new(ClaimTypes.Role, "Administrador")
        };

        var identity = new ClaimsIdentity(claims, "InnovaFemIA.Admin");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("InnovaFemIA.Admin", principal, new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
        });

        // 1. Si viene un returnUrl local válido, respetarlo asegurando el prefijo
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            if (!string.IsNullOrEmpty(pathBase) && !returnUrl.StartsWith(pathBase, StringComparison.OrdinalIgnoreCase))
            {
                returnUrl = pathBase.TrimEnd('/') + "/" + returnUrl.TrimStart('/');
            }
            return Redirect(returnUrl);
        }

        // 2. Redirección explícita y forzada con el prefijo /innovafemia al Dashboard
        var dashboardUrl = string.IsNullOrEmpty(pathBase) 
            ? "/admin/dashboard" 
            : $"{pathBase}/admin/dashboard";

        return Redirect(dashboardUrl);
    }

    [HttpGet("logout")]
    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = "InnovaFemIA.Admin")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        var pathBase = ResolvePathBase(HttpContext);
        await HttpContext.SignOutAsync("InnovaFemIA.Admin");

        var loginUrl = string.IsNullOrEmpty(pathBase)
            ? "/admin/login"
            : $"{pathBase}/admin/login";

        return Redirect(loginUrl);
    }

    [HttpGet("acceso-denegado")]
    [AllowAnonymous]
    public IActionResult AccesoDenegado()
    {
        return View("~/Views/Admin/AccesoDenegado.cshtml");
    }

    /// <summary>
    /// Resuelve de forma infalible el prefijo de ruta (ej. /innovafemia) considerando cabeceras de proxy,
    /// referer, variables de entorno y el valor predeterminado del sistema.
    /// </summary>
    public static string ResolvePathBase(HttpContext context)
    {
        // 1. Si PathBase ya fue asignado en Request por el middleware
        if (!string.IsNullOrEmpty(context.Request.PathBase.Value))
        {
            return context.Request.PathBase.Value.TrimEnd('/');
        }

        // 2. Cabecera directa del proxy inverso
        if (context.Request.Headers.TryGetValue("X-Forwarded-Prefix", out var prefixHeader))
        {
            var p = prefixHeader.ToString().Trim().TrimEnd('/');
            if (!string.IsNullOrEmpty(p)) return p.StartsWith('/') ? p : "/" + p;
        }

        // 3. Referer del navegador (si la petición proviene de https://.../innovafemia/...)
        if (context.Request.Headers.TryGetValue("Referer", out var refererHeader))
        {
            var referer = refererHeader.ToString();
            if (referer.Contains("/innovafemia", StringComparison.OrdinalIgnoreCase))
            {
                return "/innovafemia";
            }
        }

        // 4. Variable de entorno PATH_BASE
        var envPathBase = Environment.GetEnvironmentVariable("PATH_BASE");
        if (!string.IsNullOrWhiteSpace(envPathBase))
        {
            var p = envPathBase.Trim().TrimEnd('/');
            if (!string.IsNullOrEmpty(p) && p != "/") return p.StartsWith('/') ? p : "/" + p;
        }

        // 5. Valor por defecto de producción para el servidor de ICESI
        return "/innovafemia";
    }
}
