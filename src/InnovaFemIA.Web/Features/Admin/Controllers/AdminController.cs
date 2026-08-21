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
        if (User.Identity?.IsAuthenticated == true && User.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "Administrador"))
        {
            return RedirectToAction("Dashboard");
        }

        ViewBag.ReturnUrl = returnUrl;
        return View("~/Views/Admin/Login.cshtml");
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string usuario, string contrasena, string? returnUrl = null)
    {
        var result = await _mediator.Send(new AdminLoginCommand(usuario, contrasena));

        if (!result.IsSuccess)
        {
            ViewBag.Error = result.Error;
            ViewBag.ReturnUrl = returnUrl;
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

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Dashboard");
    }

    [HttpGet("logout")]
    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = "InnovaFemIA.Admin")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("InnovaFemIA.Admin");
        return RedirectToAction("Login");
    }

    [HttpGet("acceso-denegado")]
    [AllowAnonymous]
    public IActionResult AccesoDenegado()
    {
        return View("~/Views/Admin/AccesoDenegado.cshtml");
    }
}
