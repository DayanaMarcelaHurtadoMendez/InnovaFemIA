using InnovaFemIA.Web.Features.Admin.GestionCultivos;
using InnovaFemIA.Web.Features.Admin.GestionReglasLunares;
using InnovaFemIA.Web.Features.Shared.Entities;
using Microsoft.AspNetCore.Mvc;

namespace InnovaFemIA.Web.Features.Admin.Controllers;

public partial class AdminDashboardController
{
    // ── GESTIÓN DE CULTIVOS DEL SISTEMA (ADMIN) ──────────────────────────────
    [HttpGet("cultivos")]
    public async Task<IActionResult> Cultivos()
    {
        var result = await _mediator.Send(new ObtenerCultivosAdminQuery());
        return View("~/Views/Admin/Cultivos.cshtml", result.Value);
    }

    [HttpPost("cultivos/crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearCultivo([FromForm] CrearCultivoCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Cultivo registrado exitosamente.";
        return RedirectToAction("Cultivos");
    }

    [HttpPost("cultivos/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarCultivo([FromForm] EditarCultivoCommand command)
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
        var result = await _mediator.Send(new EliminarCultivoCommand(id));
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        else TempData["Exito"] = "Cultivo eliminado exitosamente.";
        return RedirectToAction("Cultivos");
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
}
