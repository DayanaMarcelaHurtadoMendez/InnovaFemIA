using InnovaFemIA.Web.Features.Admin.AdminDashboard;
using InnovaFemIA.Web.Features.Admin.GestionRegistros;
using InnovaFemIA.Web.Features.Shared.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InnovaFemIA.Web.Features.Admin.Controllers;

/// <summary>
/// Controlador principal del Panel de Administración de InnovaFemIA.
/// Modularizado mediante clases parciales por feature (Chatbot, Familias, Biocomercio/Cultivos).
/// </summary>
[Route("admin")]
[Authorize(AuthenticationSchemes = "InnovaFemIA.Admin", Roles = "Administrador")]
public partial class AdminDashboardController : Controller
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
